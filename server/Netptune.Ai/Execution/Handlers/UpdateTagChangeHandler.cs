using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateTagChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public const string ChangedSinceMessage =
        "The tag has been renamed since this change was applied, so it was left as it is now.";

    private readonly IMediator Mediator;

    public UpdateTagChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => UpdateTagAction.Change;

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tags.Update,
    };

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var tagId = AiChangePayload.ReadInt(payload, "tagId") ?? change.EntityId;

        if (!tagId.HasValue)
        {
            return AiChangePayload.Failure(change, "The tag this change refers to could not be resolved.");
        }

        var name = AiChangePayload.ReadString(payload, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiChangePayload.Failure(change, "The new tag name is missing from this change.");
        }

        var tags = await ReadTags(cancellationToken);
        var tag = tags.FirstOrDefault(item => item.Id == tagId.Value);

        if (tag is null)
        {
            return AiChangePayload.Failure(change, "The tag no longer exists in this workspace.");
        }

        var clash = FindClash(tags, tag.Id, name!);

        if (clash is not null)
        {
            return AiChangePayload.Failure(change, $"A tag named “{clash.Name}” already exists.");
        }

        var error = await Rename(tag.Name, name!, cancellationToken);

        if (error is not null)
        {
            return AiChangePayload.Failure(change, error);
        }

        return AiChangePayload.Applied(change, tag.Id);
    }

    public async Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var tagId = AiChangePayload.ReadInt(change.Payload.RootElement, "tagId") ?? change.EntityId;

        if (!tagId.HasValue)
        {
            return null;
        }

        var tags = await ReadTags(cancellationToken);
        var tag = tags.FirstOrDefault(item => item.Id == tagId.Value);

        if (tag is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(new { tagId = tag.Id, name = tag.Name });
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = change.UndoPayload?.RootElement;

        if (!snapshot.HasValue)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var tagId = AiChangePayload.ReadInt(snapshot.Value, "tagId");
        var previousName = AiChangePayload.ReadString(snapshot.Value, "name");
        var hasPreviousName = !string.IsNullOrWhiteSpace(previousName);

        if (!tagId.HasValue || !hasPreviousName)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var tags = await ReadTags(cancellationToken);
        var tag = tags.FirstOrDefault(item => item.Id == tagId.Value);

        if (tag is null)
        {
            return AiChangeUndoResult.Failure(change, "The tag is no longer in this workspace.");
        }

        var appliedName = AiChangePayload.ReadString(change.Payload.RootElement, "name")?.Trim();
        var renamedSince = !string.Equals(tag.Name, appliedName, StringComparison.Ordinal);

        if (renamedSince)
        {
            return AiChangeUndoResult.Failure(change, ChangedSinceMessage);
        }

        var clash = FindClash(tags, tag.Id, previousName!);

        if (clash is not null)
        {
            return AiChangeUndoResult.Failure(change, $"Another tag is now named “{clash.Name}”.");
        }

        var error = await Rename(tag.Name, previousName!, cancellationToken);

        if (error is not null)
        {
            return AiChangeUndoResult.Failure(change, error);
        }

        return AiChangeUndoResult.Undone(change, tag.Id);
    }

    private async Task<List<TagViewModel>> ReadTags(CancellationToken cancellationToken)
    {
        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        return tags ?? [];
    }

    private static TagViewModel? FindClash(List<TagViewModel> tags, int tagId, string name)
    {
        return tags.FirstOrDefault(item =>
            item.Id != tagId && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> Rename(string currentName, string newName, CancellationToken cancellationToken)
    {
        var request = new UpdateTagRequest { CurrentValue = currentName, NewValue = newName };
        var response = await Mediator.Send(new UpdateTagCommand(request), cancellationToken);

        if (response.IsSuccess)
        {
            return null;
        }

        return response.Message ?? "The tag could not be renamed.";
    }
}
