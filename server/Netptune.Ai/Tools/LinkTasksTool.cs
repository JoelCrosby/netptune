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
        + "Name the relation type with relationType — its ids differ between workspaces, so never guess one. "
        + "The link is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task the relation is stated from." },
          "taskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, instead of taskId." },
          "relatedTaskId": { "type": "integer", "description": "The id of the task on the other end of the relation." },
          "relatedTaskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, instead of relatedTaskId." },
          "relationType": { "type": "string", "description": "The relation type by name, such as \"blocks\" or \"relates to\". Its inverse name, such as \"is blocked by\", states the relation the other way round and swaps the two tasks." },
          "relationTypeId": { "type": "integer", "description": "The relation type id, from list_relation_types, instead of relationType." },
          "relationTypeRef": { "type": "string", "description": "Handle of a relation type proposed earlier in this change set, instead of relationType." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var relation = await ResolveRelationType(arguments, cancellationToken);

        if (relation.Error is not null)
        {
            return AiToolExecution.Failed(relation.Error);
        }

        var stated = await ResolveEnd(arguments, "taskId", "taskRef", cancellationToken);

        if (stated.Error is not null)
        {
            return AiToolExecution.Failed(stated.Error);
        }

        var related = await ResolveEnd(arguments, "relatedTaskId", "relatedTaskRef", cancellationToken);

        if (related.Error is not null)
        {
            return AiToolExecution.Failed(related.Error);
        }

        var isSameTask = stated.Label == related.Label;

        if (isSameTask)
        {
            return AiToolExecution.Failed("A task cannot be linked to itself.");
        }

        var (source, target) = relation.IsInverse ? (related, stated) : (stated, related);

        var payload = new
        {
            sourceSystemId = source.SystemId,
            sourceRef = source.RefKey,
            targetSystemId = target.SystemId,
            targetRef = target.RefKey,
            relationTypeId = relation.RelationTypeId,
            relationTypeRef = relation.RefKey,
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = source.TaskId,
            Summary = $"Link “{source.Label}” {relation.Label.ToLowerInvariant()} “{target.Label}”",
            Fields =
            [
                AiChangeFields.Values(
                    relation.Label,
                    AiChangeValueKind.Task,
                    [],
                    [AiChangeFields.Task(target.TaskId, null, target.Label)]),
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed linking {source.Label} to {target.Label}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private sealed record LinkRelation
    {
        public int? RelationTypeId { get; init; }

        public string? RefKey { get; init; }

        public string Label { get; init; } = string.Empty;

        public bool IsInverse { get; init; }

        public string? Error { get; init; }

        public static LinkRelation Failed(string error)
        {
            return new LinkRelation { Error = error };
        }
    }

    private async Task<LinkRelation> ResolveRelationType(JsonElement arguments, CancellationToken cancellationToken)
    {
        var relationTypeRef = AiPendingReference.Read(arguments, "relationTypeRef");

        if (relationTypeRef is not null)
        {
            var pending = AiPendingReference.Find(ChangeSet, relationTypeRef, AiRelationTypeLookup.EntityType);

            if (pending is null)
            {
                return LinkRelation.Failed(AiPendingReference.Missing(relationTypeRef, "relation type"));
            }

            var proposedName = CreateRelationTypeTool.ReadProposedName(pending);

            return new LinkRelation { RefKey = relationTypeRef, Label = proposedName ?? "related to" };
        }

        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        if (relationTypes is null)
        {
            return LinkRelation.Failed("Relation types could not be read.");
        }

        var relationTypeId = AiToolSchema.GetInt(arguments, "relationTypeId");

        if (relationTypeId.HasValue)
        {
            var byId = relationTypes.FirstOrDefault(relationType => relationType.Id == relationTypeId.Value);

            if (byId is null)
            {
                return LinkRelation.Failed(
                    $"Relation type {relationTypeId} is not in this workspace. "
                    + AiRelationTypeLookup.Describe(relationTypes));
            }

            return new LinkRelation { RelationTypeId = byId.Id, Label = byId.Name };
        }

        var name = AiToolSchema.GetString(arguments, "relationType")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return LinkRelation.Failed($"A relationType is required. {AiRelationTypeLookup.Describe(relationTypes)}");
        }

        var match = AiRelationTypeLookup.Match(relationTypes, name!);

        if (match is null)
        {
            return LinkRelation.Failed(
                $"“{name}” is not a relation type in this workspace. {AiRelationTypeLookup.Describe(relationTypes)}");
        }

        return new LinkRelation
        {
            RelationTypeId = match.RelationType.Id,
            Label = match.RelationType.Name,
            IsInverse = match.IsInverse,
        };
    }

    private sealed record LinkEnd(int? TaskId, string? SystemId, string? RefKey, string Label, string? Error)
    {
        public static LinkEnd Failed(string error)
        {
            return new LinkEnd(null, null, null, string.Empty, error);
        }
    }

    private async Task<LinkEnd> ResolveEnd(
        JsonElement arguments,
        string idName,
        string refName,
        CancellationToken cancellationToken)
    {
        var taskRef = AiPendingReference.Read(arguments, refName);

        if (taskRef is not null)
        {
            var pending = AiPendingReference.Find(ChangeSet, taskRef, "task");

            if (pending is null)
            {
                return LinkEnd.Failed(AiPendingReference.Missing(taskRef, "task"));
            }

            return new LinkEnd(null, null, taskRef, pending.Summary, null);
        }

        var taskId = AiToolSchema.GetInt(arguments, idName);

        if (!taskId.HasValue)
        {
            return LinkEnd.Failed($"A {idName} or {refName} is required.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return LinkEnd.Failed($"Task {taskId} was not found in this workspace.");
        }

        return new LinkEnd(task.Id, task.SystemId, null, $"{task.SystemId} · {task.Name}", null);
    }
}
