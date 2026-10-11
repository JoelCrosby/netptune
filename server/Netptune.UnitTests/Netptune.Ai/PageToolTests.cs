using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.Search;
using Netptune.Handlers.Pages.Queries;
using Netptune.Handlers.Search.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class PageToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task SearchPages_ShouldSearchOnlyPages_AndAskForThem()
    {
        GivenSearchResults();

        await Execute(new SearchPagesTool(Mediator), """{"query":"runbook","limit":50}""");

        await Mediator.Received(1).Send(
            Arg.Is<SearchQuery>(query =>
                query.Q == "runbook"
                && query.IncludePages
                && query.Limit == 25
                && query.Types!.SequenceEqual(new[] { "pages" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchPages_ShouldReturnTheSlugPathAndSnippet_OfEachPage()
    {
        GivenSearchResults();

        var result = await Execute(new SearchPagesTool(Mediator), """{"query":"runbook"}""");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);
        var page = content.RootElement.EnumerateArray().Single();

        page.GetProperty("id").GetInt32().Should().Be(12);
        page.GetProperty("slug").GetString().Should().Be("runbook-ab12");
        page.GetProperty("name").GetString().Should().Be("Runbook");
        page.GetProperty("path").GetString().Should().Be("Engineering");
        page.GetProperty("snippet").GetString().Should().Be("…rotate the keys…");
    }

    [Fact]
    public async Task SearchPages_ShouldFail_WhenThereIsNoQuery()
    {
        var result = await Execute(new SearchPagesTool(Mediator), """{"query":"  "}""");

        result.IsError.Should().BeTrue();
        await Mediator.DidNotReceive().Send(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPage_ShouldReadTheBodyInParts()
    {
        var body = string.Join(' ', Enumerable.Repeat("word", 50));

        GivenPage(body);

        var first = await Execute(new GetPageTool(Mediator), """{"slug":"runbook-ab12","take":100}""");

        using var content = JsonDocument.Parse(first.Content);

        content.RootElement.GetProperty("name").GetString().Should().Be("Runbook");
        content.RootElement.GetProperty("totalCharacters").GetInt32().Should().Be(body.Length);
        content.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        content.RootElement.GetProperty("body").GetString()!.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task GetPage_ShouldFail_WhenThePageIsNotInTheWorkspace()
    {
        Mediator
            .Send(Arg.Any<GetPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.NotFound);

        var result = await Execute(new GetPageTool(Mediator), """{"slug":"missing"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("missing");
    }

    [Fact]
    public async Task ListArchivedPages_ShouldReturnWhoArchivedEachPageAndWhen()
    {
        var archivedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        var archived = new ArchivedPageViewModel
        {
            Id = 20,
            Title = "Old plan",
            Slug = "old-plan",
            Kind = PageKind.Folder,
            ArchivedAt = archivedAt,
            ArchivedByDisplayName = "Ada",
        };

        Mediator
            .Send(Arg.Any<GetArchivedPagesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<List<ArchivedPageViewModel>>.Success([archived]));

        var result = await Execute(new ListArchivedPagesTool(Mediator), "{}");

        using var content = JsonDocument.Parse(result.Content);
        var page = content.RootElement.EnumerateArray().Single();

        page.GetProperty("id").GetInt32().Should().Be(20);
        page.GetProperty("title").GetString().Should().Be("Old plan");
        page.GetProperty("kind").GetString().Should().Be("folder");
        page.GetProperty("archivedAt").GetDateTime().Should().Be(archivedAt);
        page.GetProperty("archivedBy").GetString().Should().Be("Ada");
    }

    [Fact]
    public async Task ListPageVersions_ShouldMarkTheVersionThatMatchesThePageNow()
    {
        PageTestTree.Given(Mediator);
        Mediator
            .Send(Arg.Is<GetPageQuery>(query => query.Slug == "rfc"), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Success(new PageViewModel { Id = 11, Title = "RFC", Slug = "rfc", Revision = 4 }));
        Mediator
            .Send(Arg.Is<GetPageVersionsQuery>(query => query.PageId == 11), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<List<PageVersionSummaryViewModel>>.Success(
            [
                new PageVersionSummaryViewModel { Id = 31, Revision = 4, Title = "RFC", CreatedByDisplayName = "Ada" },
                new PageVersionSummaryViewModel { Id = 30, Revision = 2, Title = "RFC draft" },
            ]));

        var result = await Execute(new ListPageVersionsTool(Mediator), """{"pageId":11}""");

        using var content = JsonDocument.Parse(result.Content);
        var versions = content.RootElement.EnumerateArray().ToList();

        versions.Select(version => version.GetProperty("id").GetInt32()).Should().Equal(31, 30);
        versions.Select(version => version.GetProperty("isCurrent").GetBoolean()).Should().Equal(true, false);
        versions[0].GetProperty("author").GetString().Should().Be("Ada");
    }

    [Fact]
    public async Task ListPageVersions_ShouldRefuse_APageThatIsNotLive()
    {
        PageTestTree.Given(Mediator);

        var result = await Execute(new ListPageVersionsTool(Mediator), """{"pageId":20}""");

        result.IsError.Should().BeTrue();
    }

    private void GivenSearchResults()
    {
        var response = new SearchResponse
        {
            Results =
            [
                new SearchResultViewModel
                {
                    Type = "page",
                    Id = 12,
                    Title = "Runbook",
                    Subtitle = "Engineering",
                    Url = "/acme/pages/runbook-ab12",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["slug"] = "runbook-ab12",
                        ["kind"] = "Document",
                        ["path"] = "Engineering",
                        ["snippet"] = "…rotate the keys…",
                    },
                },
            ],
        };

        Mediator.Send(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Returns(response);
    }

    private void GivenPage(string body)
    {
        var page = new PageViewModel
        {
            Id = 12,
            Title = "Runbook",
            Slug = "runbook-ab12",
            Kind = PageKind.Document,
            Body = body,
            Revision = 3,
        };

        Mediator
            .Send(Arg.Is<GetPageQuery>(query => query.Slug == "runbook-ab12"), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Success(page));
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string arguments)
    {
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }
}
