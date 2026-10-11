using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdatePageDetailsChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public const string ChangedSinceMessage =
        "The page's status or tags have changed since this change was applied, so they were left as they are now.";

    private readonly IMediator Mediator;

    public UpdatePageDetailsChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_page_details";

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Pages.Update,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var pageId = ResolvePageId(context);

        if (!pageId.HasValue)
        {
            return AiChangePayload.Failure(change, "The page this change refers to could not be resolved.");
        }

        var rawStatus = AiChangePayload.ReadString(payload, "status");
        var status = AiPageLookup.ParseStatus(rawStatus);
        var hasUnknownStatus = rawStatus is not null && !status.HasValue;

        if (hasUnknownStatus)
        {
            return AiChangePayload.Failure(change, "The status in this change is not draft or published.");
        }

        var setsTags = payload.TryGetProperty("tags", out _);
        var tagUpdate = setsTags ? await ResolveTagIds(payload, cancellationToken) : null;

        if (tagUpdate?.Error is { } tagError)
        {
            return AiChangePayload.Failure(change, tagError);
        }

        var request = new UpdatePageRequest { Status = status, TagIds = tagUpdate?.Ids };
        var response = await Mediator.Send(new UpdatePageCommand(pageId.Value, request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The page could not be updated.");
        }

        return AiChangePayload.Applied(change, pageId);
    }

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return AiPageUndo.CaptureDetails(Mediator, ResolvePageId(context), cancellationToken);
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = AiPageUndo.ReadSnapshot(change.UndoPayload);

        if (snapshot is null)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var payload = change.Payload.RootElement;
        var appliedStatus = AiPageLookup.ParseStatus(AiChangePayload.ReadString(payload, "status"));
        var setStatus = appliedStatus.HasValue;
        var setTags = payload.TryGetProperty("tags", out _);
        var current = await AiPageLookup.Read(Mediator, snapshot.PageId, cancellationToken);

        if (current is null)
        {
            return AiChangeUndoResult.Failure(change, "The page is no longer in this workspace.");
        }

        var statusChangedSince = setStatus && current.Status != appliedStatus;
        var appliedTagNames = AiChangePayload.ReadStringArray(payload, "tags").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentTagNames = current.Tags.Select(tag => tag.Name);
        var tagsChangedSince = setTags && !appliedTagNames.SetEquals(currentTagNames);
        var changedSinceApply = statusChangedSince || tagsChangedSince;

        if (changedSinceApply)
        {
            return AiChangeUndoResult.Failure(change, ChangedSinceMessage);
        }

        var request = new UpdatePageRequest
        {
            Status = setStatus ? snapshot.Status : null,
            TagIds = setTags ? snapshot.TagIds : null,
        };

        var response = await Mediator.Send(new UpdatePageCommand(snapshot.PageId, request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? "The page details could not be restored.");
        }

        return AiChangeUndoResult.Undone(change, snapshot.PageId);
    }

    private static int? ResolvePageId(AiChangeApplyContext context)
    {
        var payload = context.Change.Payload.RootElement;

        return AiChangePayload.ReadInt(payload, "pageId")
            ?? AiChangePayload.ResolveReference(context, "pageRef")
            ?? context.Change.EntityId;
    }

    private async Task<ResolvedTagIds> ResolveTagIds(JsonElement payload, CancellationToken cancellationToken)
    {
        var names = AiChangePayload.ReadStringArray(payload, "tags");

        if (names.Count == 0)
        {
            return new ResolvedTagIds([], null);
        }

        var existingTags = await Mediator.Send(new GetTagsByNameQuery(names), cancellationToken);
        var idsByName = existingTags
            .GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var missing = names.Where(name => !idsByName.ContainsKey(name)).ToList();

        if (missing.Count > 0)
        {
            var missingNames = string.Join(", ", missing);

            return new ResolvedTagIds(
                [],
                $"These tags do not exist in this workspace: {missingNames}. Was the change that creates them applied?");
        }

        var ids = names.Select(name => idsByName[name]).Distinct().ToList();

        return new ResolvedTagIds(ids, null);
    }

    private sealed record ResolvedTagIds(List<int> Ids, string? Error);
}
