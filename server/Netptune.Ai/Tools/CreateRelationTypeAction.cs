using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateRelationTypeAction : IAiToolAction
{
    public const string Change = "propose_create_relation_type";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateRelationTypeAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.RelationTypes.Manage };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A relation type name is required.");
        }

        var rawCategory = AiToolSchema.GetString(arguments, "category");
        var isKnownCategory = Enum.TryParse<RelationCategory>(rawCategory, true, out var category);

        if (!isKnownCategory)
        {
            return AiToolExecution.Failed("A category of Hierarchy, Dependency, Related or Duplicate is required.");
        }

        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        if (relationTypes is null)
        {
            return AiToolExecution.Failed("Relation types could not be read.");
        }

        var key = name!.ToUrlSlug();
        var hasKey = !string.IsNullOrWhiteSpace(key);

        if (!hasKey)
        {
            return AiToolExecution.Failed("A relation type name needs at least one letter or digit.");
        }

        var clash = relationTypes.FirstOrDefault(relationType => relationType.Key == key);
        var isNamedLikeExisting = AiRelationTypeLookup.Exists(relationTypes, name!);

        if (clash is not null || isNamedLikeExisting)
        {
            return AiToolExecution.Failed(
                $"A relation type named “{clash?.Name ?? name}” already exists — link with it directly. "
                + AiRelationTypeLookup.Describe(relationTypes));
        }

        var isProposed = ChangeSet.Changes.Any(change =>
            string.Equals(change.ToolName, Change, StringComparison.Ordinal) &&
            string.Equals(ReadProposedName(change), name, StringComparison.OrdinalIgnoreCase));

        if (isProposed)
        {
            return AiToolExecution.Failed($"Relation type “{name}” is already proposed in this change set.");
        }

        var inverseName = AiToolSchema.GetString(arguments, "inverseName")?.Trim();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "category", After = category.ToString() },
        };

        AiToolSchema.AddOptionalField(fields, "inverseName", inverseName);
        AiToolSchema.AddOptionalField(fields, "description", AiToolSchema.GetString(arguments, "description"));
        AiToolSchema.AddOptionalField(fields, "color", AiToolSchema.GetString(arguments, "color"));

        var refKey = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiRelationTypeLookup.EntityType,
            RefKey = refKey,
            Summary = $"Create relation type “{name}”",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating relation type “{name}” as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }

    public static string? ReadProposedName(AiChangeDraft change)
    {
        var payload = change.Payload.RootElement;
        var hasName = payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("name", out _);

        if (!hasName)
        {
            return null;
        }

        return payload.GetProperty("name").GetString()?.Trim();
    }
}
