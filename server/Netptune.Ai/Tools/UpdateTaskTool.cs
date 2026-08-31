using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.Utilities;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateTaskTool : IAiTool
{
    private const decimal SmallestTShirtSize = 1;
    private const decimal LargestTShirtSize = 5;

    private static readonly string[] ClearableFields = ["startDate", "dueDate"];

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_update_task";

    public string Description =>
        "Propose changing an existing task's name, description, status, priority, dates or estimate. "
        + "Assignees and tags have their own tools. "
        + "The change is not applied until the user reviews and applies it.";

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
          "priority": {
            "type": "string",
            "enum": ["None", "Low", "Medium", "High", "Critical"],
            "description": "New priority. Use None to take a priority off the task."
          },
          "startDate": { "type": "string", "description": "New start date as YYYY-MM-DD." },
          "dueDate": { "type": "string", "description": "New due date as YYYY-MM-DD." },
          "estimateType": {
            "type": "string",
            "enum": ["StoryPoints", "Hours", "TShirt"],
            "description": "Unit the estimate is measured in. Required when the task has no estimate yet."
          },
          "estimateValue": {
            "type": "number",
            "description": "Estimate in the unit above. T-shirt sizes are 1 to 5, from XS to XL."
          },
          "clear": {
            "type": "array",
            "items": { "type": "string", "enum": ["startDate", "dueDate"] },
            "description": "Dates to remove from the task. A date cannot be both cleared and set."
          }
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

        var cleared = ReadCleared(arguments);
        var unknownClear = cleared.FirstOrDefault(field => !ClearableFields.Contains(field));

        if (unknownClear is not null)
        {
            return AiToolExecution.Failed($"“{unknownClear}” cannot be cleared. Only startDate and dueDate can.");
        }

        var fields = new List<AiChangeField>();
        var proposedDescription = AiToolSchema.GetString(arguments, "description");

        AddChangedField(fields, "name", task.Name, AiToolSchema.GetString(arguments, "name"));
        AddChangedField(fields, "description", task.Description, proposedDescription);

        var dateMessage = AddDateFields(fields, task, arguments, cleared);

        if (dateMessage is not null)
        {
            return AiToolExecution.Failed(dateMessage);
        }

        var priorityMessage = AddPriorityField(fields, task, arguments);

        if (priorityMessage is not null)
        {
            return AiToolExecution.Failed(priorityMessage);
        }

        var estimateMessage = AddEstimateField(fields, task, arguments);

        if (estimateMessage is not null)
        {
            return AiToolExecution.Failed(estimateMessage);
        }

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

    private static List<string> ReadCleared(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = arguments.TryGetProperty("clear", out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static string? AddDateFields(
        List<AiChangeField> fields,
        TaskViewModel task,
        JsonElement arguments,
        List<string> cleared)
    {
        var startMessage = AddDateField(fields, "startDate", task.StartDate, arguments, cleared);

        if (startMessage is not null)
        {
            return startMessage;
        }

        return AddDateField(fields, "dueDate", task.DueDate, arguments, cleared);
    }

    private static string? AddDateField(
        List<AiChangeField> fields,
        string name,
        DateOnly? before,
        JsonElement arguments,
        List<string> cleared)
    {
        var raw = AiToolSchema.GetString(arguments, name);
        var isCleared = cleared.Contains(name);
        var hasValue = !string.IsNullOrWhiteSpace(raw);

        if (isCleared && hasValue)
        {
            return $"“{name}” cannot be set and cleared in the same change.";
        }

        if (isCleared)
        {
            var wasSet = before.HasValue;

            if (wasSet)
            {
                fields.Add(AiChangeFields.Date(name, before, null));
            }

            return null;
        }

        if (!hasValue)
        {
            return null;
        }

        var isParsed = DateOnly.TryParse(raw, out var parsed);

        if (!isParsed)
        {
            return $"“{name}” must be a date in YYYY-MM-DD form.";
        }

        var isUnchanged = before == parsed;

        if (!isUnchanged)
        {
            fields.Add(AiChangeFields.Date(name, before, parsed));
        }

        return null;
    }

    private static string? AddPriorityField(List<AiChangeField> fields, TaskViewModel task, JsonElement arguments)
    {
        var raw = AiToolSchema.GetString(arguments, "priority");
        var hasValue = !string.IsNullOrWhiteSpace(raw);

        if (!hasValue)
        {
            return null;
        }

        var isParsed = Enum.TryParse<TaskPriority>(raw, true, out var priority);

        if (!isParsed)
        {
            return $"“{raw}” is not a priority. Use None, Low, Medium, High or Critical.";
        }

        var before = task.Priority ?? TaskPriority.None;

        AddChangedField(fields, "priority", before.ToString(), priority.ToString());

        return null;
    }

    private static string? AddEstimateField(List<AiChangeField> fields, TaskViewModel task, JsonElement arguments)
    {
        var rawType = AiToolSchema.GetString(arguments, "estimateType");
        var value = AiToolSchema.GetDecimal(arguments, "estimateValue");
        var hasType = !string.IsNullOrWhiteSpace(rawType);

        if (!hasType && !value.HasValue)
        {
            return null;
        }

        var isParsed = !hasType || Enum.TryParse<EstimateType>(rawType, true, out _);

        if (!isParsed)
        {
            return $"“{rawType}” is not an estimate unit. Use StoryPoints, Hours or TShirt.";
        }

        var type = hasType ? Enum.Parse<EstimateType>(rawType!, true) : task.EstimateType;

        if (!type.HasValue)
        {
            return "This task has no estimate unit yet, so estimateType is required alongside estimateValue.";
        }

        var resolved = value ?? task.EstimateValue;

        if (!resolved.HasValue)
        {
            return "An estimateValue is required to give this task an estimate.";
        }

        var isTShirt = type.Value == EstimateType.TShirt;
        var isOutOfRange = resolved.Value < SmallestTShirtSize || resolved.Value > LargestTShirtSize;

        if (isTShirt && isOutOfRange)
        {
            return "T-shirt estimates run from 1 (XS) to 5 (XL).";
        }

        var before = FormatEstimate(task.EstimateType, task.EstimateValue);

        AddChangedField(fields, "estimate", before, FormatEstimate(type, resolved));

        return null;
    }

    private static string? FormatEstimate(EstimateType? type, decimal? value)
    {
        var hasEstimate = type.HasValue && value.HasValue;

        if (!hasEstimate)
        {
            return null;
        }

        return $"{value!.Value:0.##} {type!.Value}";
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

        fields.Add(AiChangeFields.Values(
            "status",
            AiChangeValueKind.Status,
            [AiChangeFields.Status(task.StatusId, task.StatusName, task.StatusColor)],
            [AiChangeFields.Status(status.Id, status.Name, status.Color)]));

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
