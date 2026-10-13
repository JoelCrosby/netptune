using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateRelationTypeAction : IAiToolAction
{
    public const string Change = "propose_update_relation_type";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateRelationTypeAction(IMediator mediator, IAiChangeSetBuilder changeSet)
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

        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken) ?? [];
        var relationType = relationTypes.FirstOrDefault(item => item.Id == relationTypeId.Value);

        if (relationType is null)
        {
            return AiToolExecution.Failed($"Relation type {relationTypeId} is not in this workspace.");
        }

        var fields = new List<AiChangeField>();
        var payload = new Dictionary<string, object> { ["relationTypeId"] = relationType.Id };
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);
        var isRenamed = hasName && !string.Equals(name, relationType.Name, StringComparison.Ordinal);

        if (isRenamed)
        {
            var key = name!.ToUrlSlug();
            var hasKey = !string.IsNullOrWhiteSpace(key);

            if (!hasKey)
            {
                return AiToolExecution.Failed("A relation type name needs at least one letter or digit.");
            }

            var clash = relationTypes.FirstOrDefault(item => item.Id != relationType.Id && item.Key == key);

            if (clash is not null)
            {
                return AiToolExecution.Failed($"A relation type named “{clash.Name}” already exists.");
            }

            fields.Add(AiChangeFields.Text("name", relationType.Name, name));
            payload["name"] = name!;
        }

        AddChangedField(fields, payload, "inverseName", relationType.InverseName, AiToolSchema.GetString(arguments, "inverseName"));
        AddChangedField(fields, payload, "description", relationType.Description, AiToolSchema.GetString(arguments, "description"));
        AddChangedField(fields, payload, "color", relationType.Color, AiToolSchema.GetString(arguments, "color"));

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this relation type.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiRelationTypeLookup.EntityType,
            EntityId = relationType.Id,
            Summary = $"Update {changedNames} on relation type “{relationType.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating relation type {relationType.Id}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static void AddChangedField(
        List<AiChangeField> fields,
        Dictionary<string, object> payload,
        string name,
        string? before,
        string? after)
    {
        var value = after?.Trim();
        var hasValue = !string.IsNullOrWhiteSpace(value);

        if (!hasValue)
        {
            return;
        }

        var isUnchanged = string.Equals(before, value, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return;
        }

        fields.Add(AiChangeFields.Text(name, before, value));
        payload[name] = value!;
    }
}
