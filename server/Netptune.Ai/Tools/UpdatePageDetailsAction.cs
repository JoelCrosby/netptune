using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Ai.Tools;

public sealed class UpdatePageDetailsAction : IAiToolAction
{
    private const PageStatus NewPageStatus = PageStatus.Draft;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdatePageDetailsAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_update_page_details";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var rawStatus = AiToolSchema.GetString(arguments, "status");
        var hasStatus = rawStatus is not null;
        var status = AiPageLookup.ParseStatus(rawStatus);
        var hasUnknownStatus = hasStatus && !status.HasValue;

        if (hasUnknownStatus)
        {
            return AiToolExecution.Failed("status must be draft or published.");
        }

        var hasTags = arguments.TryGetProperty("tags", out var tagsArgument);
        var hasMalformedTags = hasTags && tagsArgument.ValueKind != JsonValueKind.Array;

        if (hasMalformedTags)
        {
            return AiToolExecution.Failed("tags must be an array of tag names.");
        }

        var changesNothing = !hasStatus && !hasTags;

        if (changesNothing)
        {
            return AiToolExecution.Failed("Send a status, tags, or both.");
        }

        var resolved = await ResolveTarget(arguments, cancellationToken);

        if (resolved.Error is not null)
        {
            return AiToolExecution.Failed(resolved.Error);
        }

        var target = resolved.Target!;
        var page = target.Page;
        var fields = new List<AiChangeField>();
        var payload = new JsonObject();

        if (page is not null)
        {
            payload["pageId"] = page.Id;
        }

        if (target.RefKey is not null)
        {
            payload["pageRef"] = target.RefKey;
        }

        if (status is { } requestedStatus)
        {
            var currentStatus = page?.Status ?? NewPageStatus;
            var isStatusUnchanged = currentStatus == requestedStatus;

            if (!isStatusUnchanged)
            {
                var currentName = AiPageLookup.DescribeStatus(currentStatus);
                var requestedName = AiPageLookup.DescribeStatus(requestedStatus);

                fields.Add(AiChangeFields.Text("status", currentName, requestedName));
                payload["status"] = requestedName;
            }
        }

        if (hasTags)
        {
            var tagsResult = await ReadTags(arguments, page, cancellationToken);

            if (tagsResult.Error is not null)
            {
                return AiToolExecution.Failed(tagsResult.Error);
            }

            if (tagsResult.Field is not null)
            {
                fields.Add(tagsResult.Field);
                payload["tags"] = JsonSerializer.SerializeToNode(tagsResult.Names);
            }
        }

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed($"“{target.Name}” already has that status and those tags.");
        }

        var changedNames = string.Join(" and ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page?.Id,
            Summary = $"Set {changedNames} on “{target.Name}”",
            Fields = fields,
            Payload = JsonDocument.Parse(payload.ToJsonString()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed setting the {changedNames} of “{target.Name}”. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private sealed record PageTarget(PageViewModel? Page, string? RefKey, string Name);

    private sealed record TargetResult(PageTarget? Target, string? Error);

    private sealed record TagsResult(List<string> Names, AiChangeField? Field, string? Error);

    private async Task<TargetResult> ResolveTarget(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageRef = AiPendingReference.Read(arguments, "pageRef");

        if (pageRef is not null)
        {
            var pending = AiPendingReference.Find(ChangeSet, pageRef, AiPageLookup.EntityType);

            if (pending is null)
            {
                return new TargetResult(null, AiPendingReference.Missing(pageRef, AiPageLookup.EntityType));
            }

            var proposedName = AiPendingReference.ProposedName(pending);

            return new TargetResult(new PageTarget(null, pageRef, proposedName), null);
        }

        var pageId = AiToolSchema.GetInt(arguments, "pageId");

        if (!pageId.HasValue)
        {
            return new TargetResult(null, "A pageId is required, or a pageRef for a page proposed in this change set.");
        }

        var page = await AiPageLookup.Read(Mediator, pageId.Value, cancellationToken);

        if (page is null)
        {
            return new TargetResult(null, $"Page {pageId} is not in this workspace.");
        }

        return new TargetResult(new PageTarget(page, null, page.Title), null);
    }

    private async Task<TagsResult> ReadTags(JsonElement arguments, PageViewModel? page, CancellationToken cancellationToken)
    {
        var requested = AiTagVocabulary.ReadRequested(arguments).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var currentNames = page?.Tags.Select(tag => tag.Name).ToList() ?? [];
        var isUnchanged = currentNames.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(requested);

        if (isUnchanged)
        {
            return new TagsResult(requested, null, null);
        }

        var unknownError = await AiTagVocabulary.FindUnknown(Mediator, ChangeSet, requested, cancellationToken);

        if (unknownError is not null)
        {
            return new TagsResult(requested, null, unknownError);
        }

        var before = currentNames.Select(AiChangeFields.Tag).ToList();
        var after = requested.Select(AiChangeFields.Tag).ToList();
        var field = AiChangeFields.Values("tags", AiChangeValueKind.Tag, before, after);

        return new TagsResult(requested, field, null);
    }
}
