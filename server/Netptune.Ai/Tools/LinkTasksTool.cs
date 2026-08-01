using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class LinkTasksTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public LinkTasksTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_link_tasks";

    public string Description =>
        "Propose linking two tasks with a relation such as blocks or relates to. "
        + "The link is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task the relation is stated from." },
          "relatedTaskId": { "type": "integer", "description": "The id of the task on the other end of the relation." },
          "relationTypeId": { "type": "integer", "description": "The relation type id, from list_relation_types." }
        }
        """,
        "taskId",
        "relatedTaskId",
        "relationTypeId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var relatedTaskId = AiToolSchema.GetInt(arguments, "relatedTaskId");
        var relationTypeId = AiToolSchema.GetInt(arguments, "relationTypeId");

        if (!taskId.HasValue || !relatedTaskId.HasValue || !relationTypeId.HasValue)
        {
            return AiToolExecution.Failed("A taskId, relatedTaskId and relationTypeId are required.");
        }

        var isSameTask = taskId.Value == relatedTaskId.Value;

        if (isSameTask)
        {
            return AiToolExecution.Failed("A task cannot be linked to itself.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);
        var relatedTask = await Mediator.Send(new GetTaskQuery(relatedTaskId.Value), cancellationToken);

        if (task is null || relatedTask is null)
        {
            return AiToolExecution.Failed("Both tasks must exist in this workspace.");
        }

        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);
        var relationType = relationTypes?.FirstOrDefault(item => item.Id == relationTypeId.Value);

        if (relationType is null)
        {
            return AiToolExecution.Failed($"Relation type {relationTypeId} is not in this workspace.");
        }

        var payload = new
        {
            sourceSystemId = task.SystemId,
            targetSystemId = relatedTask.SystemId,
            relationTypeId = relationType.Id,
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Link “{task.Name}” {relationType.Name.ToLowerInvariant()} “{relatedTask.Name}”",
            Fields =
            [
                new AiChangeField { Name = relationType.Name, After = $"{relatedTask.SystemId} · {relatedTask.Name}" },
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed linking {task.SystemId} to {relatedTask.SystemId}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
