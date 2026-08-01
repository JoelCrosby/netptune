using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateTaskTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_update_task";

    public string Description =>
        "Propose changing an existing task. The change is not applied until the user reviews and applies it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to change." },
          "name": { "type": "string", "description": "New task name." },
          "description": { "type": "string", "description": "New task description." },
          "statusId": { "type": "integer", "description": "New status id, from list_statuses." },
          "dueDate": { "type": "string", "description": "New due date as YYYY-MM-DD." }
        }
        """,
        "taskId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");

        if (!taskId.HasValue)
        {
            return AiToolExecution.Failed("A taskId is required.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var fields = new List<AiChangeField>();

        AddChangedField(fields, "name", task.Name, AiToolSchema.GetString(arguments, "name"));
        AddChangedField(fields, "description", task.Description, AiToolSchema.GetString(arguments, "description"));
        AddChangedField(fields, "dueDate", task.DueDate?.ToString("yyyy-MM-dd"), AiToolSchema.GetString(arguments, "dueDate"));

        var statusMessage = await AddStatusField(fields, task, arguments, cancellationToken);

        if (statusMessage is not null)
        {
            return AiToolExecution.Failed(statusMessage);
        }

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this task.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Update {changedNames} on “{task.Name}”",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating task {task.Id}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private async Task<string?> AddStatusField(
        List<AiChangeField> fields,
        TaskViewModel task,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var statusId = AiToolSchema.GetInt(arguments, "statusId");

        if (!statusId.HasValue)
        {
            return null;
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var status = statuses?.FirstOrDefault(item => item.Id == statusId.Value);

        if (status is null)
        {
            return $"Status {statusId} is not in this workspace.";
        }

        var isUnchanged = task.StatusId == status.Id;

        if (isUnchanged)
        {
            return null;
        }

        fields.Add(new AiChangeField
        {
            Name = "status",
            Before = task.StatusName,
            After = status.Name,
        });

        return null;
    }

    private static void AddChangedField(
        List<AiChangeField> fields,
        string name,
        string? before,
        string? after)
    {
        var hasValue = !string.IsNullOrWhiteSpace(after);

        if (!hasValue)
        {
            return;
        }

        var isUnchanged = string.Equals(before, after, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return;
        }

        fields.Add(new AiChangeField { Name = name, Before = before, After = after });
    }
}
