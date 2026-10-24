using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class ArchivePageAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public ArchivePageAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_archive_page";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Delete };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageId = AiToolSchema.GetInt(arguments, "pageId");

        if (!pageId.HasValue)
        {
            return AiToolExecution.Failed("A pageId is required.");
        }

        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var page = tree.FirstOrDefault(node => node.Id == pageId.Value);

        if (page is null)
        {
            return AiToolExecution.Failed(
                $"Page {pageId} is not a live page in this workspace. Archived pages are listed by list_archived_pages.");
        }

        var descendantCount = AiPageLookup.CountDescendants(tree, page.Id);
        var insideNote = descendantCount switch
        {
            0 => string.Empty,
            1 => " and the 1 page inside it",
            _ => $" and the {descendantCount} pages inside it",
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page.Id,
            Summary = DescribeArchive(page.Title, descendantCount),
            Fields = [AiChangeFields.Text("name", page.Title, null)],
            Payload = JsonSerializer.SerializeToDocument(new { pageId = page.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed archiving page {page.Id}{insideNote}. It can be restored afterwards. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static string DescribeArchive(string title, int descendantCount)
    {
        return descendantCount switch
        {
            0 => Localized.Format(AppText.AiSummaryArchivePage, title),
            1 => Localized.Format(AppText.AiSummaryArchivePageWithOneInside, title),
            _ => Localized.Format(AppText.AiSummaryArchivePageWithManyInside, title, descendantCount),
        };
    }
}
