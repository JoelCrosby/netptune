using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateTagAction : IAiToolAction
{
    public const string Change = "propose_update_tag";

    public const int NameMinLength = 2;
    public const int NameMaxLength = 128;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateTagAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var tagId = AiToolSchema.GetInt(arguments, "tagId");

        if (!tagId.HasValue)
        {
            return AiToolExecution.Failed("A tagId is required.");
        }

        var name = AiToolSchema.GetString(arguments, "name")?.Trim() ?? string.Empty;
        var isValidLength = name.Length is >= NameMinLength and <= NameMaxLength;

        if (!isValidLength)
        {
            return AiToolExecution.Failed($"A tag name must be {NameMinLength} to {NameMaxLength} characters.");
        }

        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken) ?? [];
        var tag = tags.FirstOrDefault(item => item.Id == tagId.Value);

        if (tag is null)
        {
            return AiToolExecution.Failed($"Tag {tagId} is not in this workspace.");
        }

        var isUnchanged = string.Equals(tag.Name, name, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return AiToolExecution.Failed($"Tag {tag.Id} is already named “{name}”.");
        }

        var clash = tags.FirstOrDefault(item =>
            item.Id != tag.Id && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

        if (clash is not null)
        {
            return AiToolExecution.Failed($"A tag named “{clash.Name}” already exists.");
        }

        var isProposedForCreation = CreateTagAction.ProposedNames(ChangeSet)
            .Contains(name, StringComparer.OrdinalIgnoreCase);

        if (isProposedForCreation)
        {
            return AiToolExecution.Failed($"Tag “{name}” is already proposed in this change set.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = "tag",
            EntityId = tag.Id,
            Summary = $"Rename tag “{tag.Name}” to “{name}”",
            Fields = [AiChangeFields.Text("name", tag.Name, name)],
            Payload = JsonSerializer.SerializeToDocument(new { tagId = tag.Id, name }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed renaming tag {tag.Id} to “{name}”. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
