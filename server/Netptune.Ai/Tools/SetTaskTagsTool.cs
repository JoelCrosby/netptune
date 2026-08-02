using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class SetTaskTagsTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public SetTaskTagsTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_set_task_tags";

    public string Description =>
        "Propose replacing the tags on a task, by taskId or by the taskRef of a task proposed in this change set. "
        + "Tags must exist already or be proposed with propose_create_tag. Nothing is applied until the user approves it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Assign };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to tag." },
          "taskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, instead of taskId." },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "The complete set of tag names for the task. An empty array clears all tags."
          }
        }
        """,
        "tags");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var taskRef = AiPendingReference.Read(arguments, "taskRef");
        var hasTarget = taskId.HasValue || taskRef is not null;

        if (!hasTarget)
        {
            return AiToolExecution.Failed("A taskId or taskRef is required.");
        }

        var pending = taskRef is null ? null : AiPendingReference.Find(ChangeSet, taskRef, "task");

        if (taskRef is not null && pending is null)
        {
            return AiToolExecution.Failed(AiPendingReference.Missing(taskRef, "task"));
        }

        var task = taskId.HasValue
            ? await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken)
            : null;

        if (taskId.HasValue && task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var requested = ReadTags(arguments);
        var workspaceTags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        if (workspaceTags is null)
        {
            return AiToolExecution.Failed("Workspace tags could not be read.");
        }

        var knownNames = workspaceTags.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var proposed in ReadProposedTags())
        {
            knownNames.Add(proposed);
        }

        var unknown = requested.Where(tag => !knownNames.Contains(tag)).ToList();

        if (unknown.Count > 0)
        {
            return AiToolExecution.Failed(
                $"These tags do not exist in this workspace: {string.Join(", ", unknown)}. "
                + "Use propose_create_tag first if the workspace needs a new one.");
        }

        var before = task is null ? string.Empty : string.Join(", ", task.Tags);
        var after = string.Join(", ", requested);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task?.Id,
            Summary = $"Set tags on “{task?.Name ?? pending!.Summary}”",
            Fields =
            [
                new AiChangeField
                {
                    Name = "tags",
                    Before = before.Length == 0 ? "none" : before,
                    After = requested.Count == 0 ? "none" : after,
                },
            ],
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            "Proposed retagging the task. Nothing has been applied yet — the user must review and apply the change.");
    }

    private IEnumerable<string> ReadProposedTags()
    {
        return ChangeSet.Changes
            .Where(change => string.Equals(change.ToolName, CreateTagTool.ToolName, StringComparison.Ordinal))
            .Select(CreateTagTool.ReadProposedName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!);
    }

    private static List<string> ReadTags(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = arguments.TryGetProperty("tags", out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }
}
