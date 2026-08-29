using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateRelationTypeTool : IAiTool
{
    public const string ToolName = "propose_create_relation_type";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateRelationTypeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => ToolName;

    public string Description =>
        "Propose a new relation type tasks can be linked with, for a relationship no existing type covers. "
        + "Check list_relation_types first — reuse an existing type when one fits. "
        + "Once proposed, propose_link_tasks may use it in the same change set through the handle it answers with. "
        + "The relation type is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.RelationTypes.Manage };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "What the link is called read from the first task, for example \"Blocks\"." },
          "inverseName": { "type": "string", "description": "What it is called read from the other task, for example \"Is Blocked By\". A related type reads the same both ways and ignores this." },
          "category": {
            "type": "string",
            "description": "How the link behaves. Hierarchy gives each task one parent and forbids cycles, dependency forbids cycles, related reads the same both ways, duplicate marks the same work twice over.",
            "enum": ["Hierarchy", "Dependency", "Related", "Duplicate"]
          },
          "description": { "type": "string", "description": "Optional description of what the link means." },
          "color": { "type": "string", "description": "Optional colour, as a hex value or a named colour." }
        }
        """,
        "name",
        "category");

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

        var isExisting = AiRelationTypeLookup.Exists(relationTypes, name!);

        if (isExisting)
        {
            return AiToolExecution.Failed(
                $"A relation type named “{name}” already exists — link with it directly. "
                + AiRelationTypeLookup.Describe(relationTypes));
        }

        var isProposed = ChangeSet.Changes.Any(change =>
            string.Equals(change.ToolName, ToolName, StringComparison.Ordinal) &&
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
            ToolName = Name,
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
