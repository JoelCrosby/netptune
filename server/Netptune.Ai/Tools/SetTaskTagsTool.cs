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
        "Propose replacing the tags on a task. Tags must already exist in the workspace — use list_tags first. Nothing is applied until the user approves it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Assign };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to tag." },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "The complete set of tag names for the task. An empty array clears all tags."
          }
        }
        """,
        "taskId",
        "tags");

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

        var requested = ReadTags(arguments);
        var workspaceTags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        if (workspaceTags is null)
        {
            return AiToolExecution.Failed("Workspace tags could not be read.");
        }

        var knownNames = workspaceTags.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = requested.Where(tag => !knownNames.Contains(tag)).ToList();

        if (unknown.Count > 0)
        {
            return AiToolExecution.Failed($"These tags do not exist in this workspace: {string.Join(", ", unknown)}.");
        }

        var before = string.Join(", ", task.Tags);
        var after = string.Join(", ", requested);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Set tags on “{task.Name}”",
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
            $"Proposed retagging task {task.Id}. Nothing has been applied yet — the user must review and apply the change.");
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
