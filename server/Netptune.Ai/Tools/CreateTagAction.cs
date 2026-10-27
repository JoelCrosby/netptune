using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;
using Netptune.Core.Tags;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateTagAction : IAiToolAction
{
    public const string Change = "propose_create_tag";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateTagAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Create };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A tag name is required.");
        }

        name = TagNames.Normalize(name!);

        var isValidLength = name.Length is >= UpdateTagAction.NameMinLength and <= UpdateTagAction.NameMaxLength;

        if (!isValidLength)
        {
            var range = $"{UpdateTagAction.NameMinLength} to {UpdateTagAction.NameMaxLength}";

            return AiToolExecution.Failed($"A tag name must be {range} characters.");
        }

        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);
        var isExisting = tags?.Any(tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (isExisting)
        {
            return AiToolExecution.Failed($"Tag “{name}” already exists — use it directly.");
        }

        var isProposed = ChangeSet.Changes.Any(change =>
            string.Equals(change.ToolName, Change, StringComparison.Ordinal) &&
            string.Equals(ReadProposedName(change), name, StringComparison.OrdinalIgnoreCase));

        if (isProposed)
        {
            return AiToolExecution.Failed($"Tag “{name}” is already proposed in this change set.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = "tag",
            Summary = Localized.Format(AppText.AiSummaryCreateTag, name),
            Fields = [AiChangeFields.Values("tag", AiChangeValueKind.Tag, [], [AiChangeFields.Tag(name!)])],
            Payload = JsonSerializer.SerializeToDocument(new { name }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating tag “{name}”. Nothing has been applied yet — the user must review and apply the change.");
    }

    public static IEnumerable<string> ProposedNames(IAiChangeSetBuilder changeSet)
    {
        return changeSet.Changes
            .Where(change => string.Equals(change.ToolName, Change, StringComparison.Ordinal))
            .Select(ReadProposedName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!);
    }

    public static string? ReadProposedName(AiChangeDraft change)
    {
        var payload = change.Payload.RootElement;
        var hasName = payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("name", out _);

        return hasName ? payload.GetProperty("name").GetString() : null;
    }
}
