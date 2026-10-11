using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Tools;

public sealed class DeleteRelationTypeAction : IAiToolAction
{
    public const string Change = "propose_delete_relation_type";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteRelationTypeAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.RelationTypes.Manage };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var relationTypeId = AiToolSchema.GetInt(arguments, "relationTypeId");

        if (!relationTypeId.HasValue)
        {
            return AiToolExecution.Failed("A relationTypeId is required.");
        }

        var usage = await Mediator.Send(new GetRelationTypeUsageQuery(relationTypeId.Value), cancellationToken);

        if (usage is null)
        {
            return AiToolExecution.Failed($"Relation type {relationTypeId} is not in this workspace.");
        }

        if (!usage.CanDelete)
        {
            return AiToolExecution.Failed(usage.BlockedReason ?? $"Relation type “{usage.Name}” cannot be deleted.");
        }

        var fields = new List<AiChangeField> { AiChangeFields.Text("relationType", usage.Name, null) };

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiRelationTypeLookup.EntityType,
            EntityId = usage.Id,
            Summary = $"Delete relation type “{usage.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { relationTypeId = usage.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting relation type {usage.Id}. "
            + "Nothing has been deleted yet — the user must review and apply the change.");
    }
}
