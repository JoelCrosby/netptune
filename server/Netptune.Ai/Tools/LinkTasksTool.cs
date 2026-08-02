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
          "taskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, instead of taskId." },
          "relatedTaskId": { "type": "integer", "description": "The id of the task on the other end of the relation." },
          "relatedTaskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, instead of relatedTaskId." },
          "relationTypeId": { "type": "integer", "description": "The relation type id, from list_relation_types." }
        }
        """,
        "relationTypeId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var relationTypeId = AiToolSchema.GetInt(arguments, "relationTypeId");

        if (!relationTypeId.HasValue)
        {
            return AiToolExecution.Failed("A relationTypeId is required.");
        }

        var source = await ResolveEnd(arguments, "taskId", "taskRef", cancellationToken);

        if (source.Error is not null)
        {
            return AiToolExecution.Failed(source.Error);
        }

        var target = await ResolveEnd(arguments, "relatedTaskId", "relatedTaskRef", cancellationToken);

        if (target.Error is not null)
        {
            return AiToolExecution.Failed(target.Error);
        }

        var isSameTask = source.Label == target.Label;

        if (isSameTask)
        {
            return AiToolExecution.Failed("A task cannot be linked to itself.");
        }

        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);
        var relationType = relationTypes?.FirstOrDefault(item => item.Id == relationTypeId.Value);

        if (relationType is null)
        {
            return AiToolExecution.Failed($"Relation type {relationTypeId} is not in this workspace.");
        }

        var payload = new
        {
            sourceSystemId = source.SystemId,
            sourceRef = source.RefKey,
            targetSystemId = target.SystemId,
            targetRef = target.RefKey,
            relationTypeId = relationType.Id,
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = source.TaskId,
            Summary = $"Link “{source.Label}” {relationType.Name.ToLowerInvariant()} “{target.Label}”",
            Fields =
            [
                new AiChangeField { Name = relationType.Name, After = target.Label },
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed linking {source.Label} to {target.Label}. Nothing has been applied yet — the user must review and apply the change.");
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
