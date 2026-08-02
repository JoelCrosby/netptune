using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Relations.Queries;

namespace Netptune.Ai.Tools;

public sealed class UnlinkTasksTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UnlinkTasksTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_unlink_tasks";

    public string Description =>
        "Propose removing a relation between two tasks. Read the relation id from list_task_relations first. "
        + "The link is not removed until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "systemId": { "type": "string", "description": "The task's system id, for example NPT-42." },
          "relationId": { "type": "integer", "description": "The relation id, from list_task_relations on that task." }
        }
        """,
        "systemId",
        "relationId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId)
        {
            return AiToolExecution.Failed("A systemId is required.");
        }

        var relationId = AiToolSchema.GetInt(arguments, "relationId");

        if (!relationId.HasValue)
        {
            return AiToolExecution.Failed("A relationId is required.");
        }

        var relations = await Mediator.Send(new GetTaskRelationsQuery(systemId!), cancellationToken);

        if (relations is null)
        {
            return AiToolExecution.Failed($"Task {systemId} was not found in this workspace.");
        }

        var relation = relations.FirstOrDefault(item => item.Id == relationId.Value);

        if (relation is null)
        {
            return AiToolExecution.Failed($"Task {systemId} has no relation with id {relationId}.");
        }

        var related = $"{relation.RelatedTask.SystemId} · {relation.RelatedTask.Name}";
        var payload = new { relationId = relation.Id };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = relation.RelatedTask.Id,
            Summary = $"Unlink “{systemId}” from “{related}”",
            Fields =
            [
                new AiChangeField { Name = relation.RelationTypeName, Before = related },
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed removing the {relation.RelationTypeName.ToLowerInvariant()} link between {systemId} and {related}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
