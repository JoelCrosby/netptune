using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateTagTool : IAiTool
{
    public const string ToolName = "propose_create_tag";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateTagTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => ToolName;

    public string Description =>
        "Propose adding a tag to the workspace vocabulary, for a theme no existing tag covers. "
        + "Once proposed, propose_set_task_tags may use the new tag in the same change set. "
        + "The tag is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The tag name. Check list_tags first — reuse an existing tag when one fits." }
        }
        """,
        "name");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A tag name is required.");
        }

        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);
        var isExisting = tags?.Any(tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (isExisting)
        {
            return AiToolExecution.Failed($"Tag “{name}” already exists — use it directly.");
        }

        var isProposed = ChangeSet.Changes.Any(change =>
            string.Equals(change.ToolName, ToolName, StringComparison.Ordinal) &&
            string.Equals(ReadProposedName(change), name, StringComparison.OrdinalIgnoreCase));

        if (isProposed)
        {
            return AiToolExecution.Failed($"Tag “{name}” is already proposed in this change set.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "tag",
            Summary = $"Create tag “{name}”",
            Fields = [new AiChangeField { Name = "tag", After = name }],
            Payload = JsonSerializer.SerializeToDocument(new { name }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating tag “{name}”. Nothing has been applied yet — the user must review and apply the change.");
    }

    public static IEnumerable<string> ProposedNames(IAiChangeSetBuilder changeSet)
    {
        return changeSet.Changes
            .Where(change => string.Equals(change.ToolName, ToolName, StringComparison.Ordinal))
            .Select(ReadProposedName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!);
    }

    public static string? ReadProposedName(AiChangeDraft change)
    {
        var payload = change.Payload.RootElement;
        var hasName = payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("name", out var value);

        return hasName ? payload.GetProperty("name").GetString() : null;
    }
}
