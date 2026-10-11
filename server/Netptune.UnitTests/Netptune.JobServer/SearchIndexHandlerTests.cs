using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;
using Netptune.JobServer.Handlers;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.JobServer;

public class SearchIndexHandlerTests
{
    private const string WorkspaceSlug = "acme";

    private readonly IMeilisearchService SearchService = Substitute.For<IMeilisearchService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly SearchIndexHandler Handler;

    public SearchIndexHandlerTests()
    {
        Handler = new SearchIndexHandler(SearchService, UnitOfWork, NullLogger<SearchIndexHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldIndexPageDocumentsWithTheirPath_WhenPagesAreIndexed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        UnitOfWork.Pages
            .GetSearchSources(Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 42 })), WorkspaceSlug, cancellationToken)
            .Returns([Source(42, parentId: 7)]);
        UnitOfWork.Pages
            .GetAncestorOutline(Arg.Any<IReadOnlyCollection<int>>(), WorkspaceSlug, cancellationToken)
            .Returns([new PageOutlineEntry(7, null, "Handbook")]);

        await Handler.Handle(PageEvent(SearchIndexOperation.Index, 42), cancellationToken);

        await SearchService.Received(1).IndexDocumentsAsync(
            "pages",
            Arg.Is<IEnumerable<PageSearchDocument>>(documents =>
                documents.Single().Id == "page_42" && documents.Single().Path == "Handbook"),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldNotIndex_WhenNoneOfThePagesAreLive()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        UnitOfWork.Pages.GetSearchSources(Arg.Any<IReadOnlyCollection<int>>(), WorkspaceSlug, cancellationToken).Returns([]);

        await Handler.Handle(PageEvent(SearchIndexOperation.Index, 42), cancellationToken);

        await UnitOfWork.Pages.DidNotReceive().GetAncestorOutline(
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await SearchService.DidNotReceive().IndexDocumentsAsync(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<PageSearchDocument>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDeletePageDocumentsById_WhenPagesAreRemoved()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await Handler.Handle(PageEvent(SearchIndexOperation.Delete, 42, 43), cancellationToken);

        await SearchService.Received(1).DeleteDocumentsAsync(
            "pages",
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "page_42", "page_43" })),
            cancellationToken);
    }

    private static SearchIndexEvent PageEvent(SearchIndexOperation operation, params int[] pageIds)
    {
        return new SearchIndexEvent
        {
            Operation = operation,
            EntityType = SearchIndexEntityTypes.Page,
            EntityIds = pageIds,
            WorkspaceSlug = WorkspaceSlug,
        };
    }

    private static PageSearchSource Source(int id, int? parentId)
    {
        return new PageSearchSource
        {
            Id = id,
            ParentId = parentId,
            Kind = PageKind.Document,
            Title = "Runbook",
            Slug = "runbook",
            Status = PageStatus.Draft,
        };
    }
}
