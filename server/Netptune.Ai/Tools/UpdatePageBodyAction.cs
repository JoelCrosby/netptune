using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdatePageBodyAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdatePageBodyAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_update_page_body";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageId = AiToolSchema.GetInt(arguments, "pageId");

        if (!pageId.HasValue)
        {
            return AiToolExecution.Failed("A pageId is required.");
        }

        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var node = tree.FirstOrDefault(candidate => candidate.Id == pageId.Value);

        if (node is null)
        {
            return AiToolExecution.Failed($"Page {pageId} is not in this workspace.");
        }

        if (node.Kind == PageKind.Folder)
        {
            return AiToolExecution.Failed("Folders have no body to update.");
        }

        var readRevision = AiToolSchema.GetInt(arguments, "revision");

        if (!readRevision.HasValue)
        {
            return AiToolExecution.Failed("A revision is required: the one get_page returned when you read the page.");
        }

        var body = AiToolSchema.GetString(arguments, "body");

        if (body is null)
        {
            return AiToolExecution.Failed("A body is required: the whole new body, as markdown.");
        }

        var bodyTooLarge = PageBody.IsTooLarge(body);

        if (bodyTooLarge)
        {
            return AiToolExecution.Failed(PageBody.TooLargeMessage);
        }

        var result = await Mediator.Send(new GetPageQuery(node.Slug), cancellationToken);
        var page = result.Payload;
        var isFound = result.IsSuccess && page is not null;

        if (!isFound)
        {
            return AiToolExecution.Failed($"Page {pageId} is not in this workspace.");
        }

        // The body was written from what the model read; someone's edit since then would be overwritten unseen.
        var changedSinceRead = page!.Revision != readRevision.Value;

        if (changedSinceRead)
        {
            return AiToolExecution.Failed(
                $"Page {page.Id} changed since you read it (now revision {page.Revision}). Read it again with get_page and propose from that.");
        }

        var requestedTitle = AiToolSchema.GetString(arguments, "title");
        var hasRequestedTitle = !string.IsNullOrWhiteSpace(requestedTitle);
        var title = hasRequestedTitle ? PageTitle.Normalise(requestedTitle) : page.Title;
        var titleTooLong = PageTitle.IsTooLong(title);

        if (titleTooLong)
        {
            return AiToolExecution.Failed(PageTitle.TooLongMessage);
        }

        var taskHandles = AiPageLookup.ReadTaskHandles(body);
        var unknownHandle = taskHandles.FirstOrDefault(handle =>
        {
            return AiPendingReference.Find(ChangeSet, handle, "task") is null;
        });

        if (unknownHandle is not null)
        {
            return AiToolExecution.Failed(AiPendingReference.Missing(unknownHandle, "task"));
        }

        var currentBody = page.Body ?? string.Empty;
        var isRenamed = !string.Equals(title, page.Title, StringComparison.Ordinal);
        var isRewritten = !string.Equals(body, currentBody, StringComparison.Ordinal);

        if (!isRenamed && !isRewritten)
        {
            return AiToolExecution.Failed("The proposed body and title match the page as it is.");
        }

        var fields = new List<AiChangeField>();

        if (isRenamed)
        {
            fields.Add(AiChangeFields.Text("name", page.Title, title));
        }

        if (isRewritten)
        {
            fields.Add(AiChangeFields.Text("body", currentBody, body));
        }

        // The revision the model read, so applying after anyone else's edit is a conflict.
        var payload = new Dictionary<string, object>
        {
            ["pageId"] = page.Id,
            ["title"] = title,
            ["body"] = body,
            ["revision"] = readRevision.Value,
        };

        // Named so the applier waits for those tasks, and skips the rewrite if any of them failed.
        if (taskHandles.Count > 0)
        {
            payload["taskRefs"] = taskHandles;
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page.Id,
            Summary = isRewritten ? $"Rewrite “{page.Title}”" : $"Rename “{page.Title}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed replacing the body of page {page.Id}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
