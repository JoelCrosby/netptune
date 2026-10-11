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
