using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

public sealed class AssignTaskTool : IAiTool
{
    private const int MemberLookupPageSize = 200;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public AssignTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_assign_task";

    public string Description =>
        "Propose replacing the assignees on a task. Use list_members to find assignee ids first. Nothing is applied until the user approves it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Reassign };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to reassign." },
          "assigneeIds": {
            "type": "array",
            "items": { "type": "string" },
            "description": "The complete set of assignee ids for the task. An empty array clears all assignees."
          }
        }
        """,
        "taskId",
        "assigneeIds");

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

        var assigneeIds = ReadAssigneeIds(arguments);
        var members = await LoadMembers(cancellationToken);

        if (members is null)
        {
            return AiToolExecution.Failed("Workspace members could not be read.");
        }

        var unknownIds = assigneeIds.Where(id => !members.ContainsKey(id)).ToList();

        if (unknownIds.Count > 0)
        {
            return AiToolExecution.Failed($"These assignee ids are not in this workspace: {string.Join(", ", unknownIds)}.");
        }

        var before = string.Join(", ", task.Assignees.Select(assignee => assignee.DisplayName));
        var after = string.Join(", ", assigneeIds.Select(id => members[id]));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Set assignees on “{task.Name}”",
            Fields =
            [
                new AiChangeField
                {
                    Name = "assignees",
                    Before = before,
                    After = assigneeIds.Count == 0 ? "none" : after,
                },
            ],
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed reassigning task {task.Id}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static List<string> ReadAssigneeIds(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = arguments.TryGetProperty("assigneeIds", out var value)
            && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private async Task<Dictionary<string, string>?> LoadMembers(CancellationToken cancellationToken)
    {
        var filter = new AssigneeFilter { Page = 1, PageSize = MemberLookupPageSize };
        var result = await Mediator.Send(new GetAssigneesQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return null;
        }

        var members = result.Payload?.Items ?? [];

        return members.ToDictionary(member => member.Id, member => member.DisplayName, StringComparer.Ordinal);
    }
}
