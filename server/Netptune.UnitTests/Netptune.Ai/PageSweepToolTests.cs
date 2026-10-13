using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class PageSweepToolTests
{
    private const string WrappedBody =
        "# RFC\n\nThis paragraph was wrapped by hand\nso it carries on here\nand once more.\n\n- an item\n- another item\n";

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    public PageSweepToolTests()
    {
        PageTestTree.Given(Mediator);

        List<PageBodyViewModel> bodies =
        [
            new(10, string.Empty),
            new(11, WrappedBody),
            new(12, "One line of prose that mentions the API key rotation."),
            new(13, "Welcome.\n\n```\nlong code line\nmore code\n```\n\nRotate the api key yearly."),
        ];

        Mediator
            .Send(Arg.Any<GetPageBodiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<List<PageBodyViewModel>>.Success(bodies));
    }

    [Fact]
    public void FormatReader_ShouldCountParagraphContinuations_ButNotListItemsOrCode()
    {
        var format = AiTextFormatReader.Read(WrappedBody);

        format.WrappedLines.Should().Be(2);
        format.ProseLines.Should().Be(6);
        format.LongestLine.Should().Be("This paragraph was wrapped by hand".Length);
    }

    [Fact]
    public void FormatReader_ShouldIgnoreLinesInsideFencedCode()
    {
        var format = AiTextFormatReader.Read("Intro.\n\n```\nfirst\nsecond\nthird\n```\n");

        format.WrappedLines.Should().Be(0);
        format.ProseLines.Should().Be(1);
    }

    [Fact]
    public void FormatReader_ShouldNotCountLinesAfterAnExplicitHardBreak()
    {
        var format = AiTextFormatReader.Read("Line one  \nline two\\\nline three");

        format.WrappedLines.Should().Be(0);
    }

    [Fact]
    public async Task ListPages_ShouldListEveryPageInTreeOrder_WithItsPathAndFormat()
    {
        var result = await Execute(new ListPagesTool(Mediator), "{}");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;
        var pages = root.GetProperty("pages").EnumerateArray().ToList();

        root.GetProperty("totalCount").GetInt32().Should().Be(4);
        root.GetProperty("hasMore").GetBoolean().Should().BeFalse();
        pages.Select(page => page.GetProperty("id").GetInt32()).Should().Equal(10, 11, 12, 13);
        pages[1].GetProperty("path").GetString().Should().Be("Engineering");
        pages[1].GetProperty("format").GetProperty("wrappedLines").GetInt32().Should().Be(2);
        pages[3].GetProperty("path").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ListPages_ShouldOnlyReadTheBodiesOfThePageItReturns()
    {
        var result = await Execute(new ListPagesTool(Mediator), """{"pageSize":2}""");

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        await Mediator.Received(1).Send(
            Arg.Is<GetPageBodiesQuery>(query => query.Ids.SequenceEqual(new[] { 10, 11 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPages_ShouldReturnTheLastPage_WithoutMore()
    {
        var result = await Execute(new ListPagesTool(Mediator), """{"page":2,"pageSize":3}""");

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;
        var ids = root.GetProperty("pages").EnumerateArray().Select(page => page.GetProperty("id").GetInt32());

        ids.Should().Equal(13);
        root.GetProperty("hasMore").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ListPages_ShouldOnlyListPagesInsideTheFolder()
    {
        var result = await Execute(new ListPagesTool(Mediator), """{"folderId":10}""");

        using var content = JsonDocument.Parse(result.Content);
        var ids = content.RootElement.GetProperty("pages").EnumerateArray().Select(page => page.GetProperty("id").GetInt32());

        ids.Should().Equal(11, 12);
    }

    [Fact]
    public async Task ListPages_ShouldRefuse_AFolderThatIsNotLive()
    {
        var result = await Execute(new ListPagesTool(Mediator), """{"folderId":99}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task GetPages_ShouldReadEachPage_AndReportTheOnesThatAreMissing()
    {
        GivenPage("rfc", 11, WrappedBody);
        Mediator
            .Send(Arg.Is<GetPageQuery>(query => query.Slug == "gone"), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.NotFound);

        var result = await Execute(new GetPagesTool(Mediator), """{"slugs":["rfc","gone"]}""");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);
        var page = content.RootElement.GetProperty("pages").EnumerateArray().Single();

        page.GetProperty("slug").GetString().Should().Be("rfc");
        page.GetProperty("body").GetString().Should().Be(WrappedBody);
        page.GetProperty("format").GetProperty("wrappedLines").GetInt32().Should().Be(2);
        content.RootElement.GetProperty("missing").EnumerateArray().Select(slug => slug.GetString()).Should().Equal("gone");
    }

    [Fact]
    public async Task GetPages_ShouldShareTheBudgetBetweenPages()
    {
        var body = new string('a', 20000);
        var slugs = Enumerable.Range(1, 10).Select(index => $"page-{index}").ToList();

        foreach (var slug in slugs)
        {
            GivenPage(slug, 1, body);
        }

        var arguments = JsonSerializer.Serialize(new { slugs, take = 20000 });
        var result = await Execute(new GetPagesTool(Mediator), arguments);

        using var content = JsonDocument.Parse(result.Content);
        var lengths = content.RootElement.GetProperty("pages").EnumerateArray().Select(page => page.GetProperty("body").GetString()!.Length);

        lengths.Should().AllSatisfy(length => length.Should().Be(2400));
    }

    [Theory]
    [InlineData("""{"slugs":[]}""")]
    [InlineData("""{"slugs":["a","b","c","d","e","f","g","h","i","j","k"]}""")]
    public async Task GetPages_ShouldRefuse_TooFewOrTooManySlugs(string arguments)
    {
        var result = await Execute(new GetPagesTool(Mediator), arguments);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task FindInPages_ShouldSendThePatternAndFolderScope_ToTheDatabase()
    {
        GivenFoundPages(total: 0);

        await Execute(new FindInPagesTool(Mediator), """{"pattern":"key","regex":true,"caseSensitive":true,"folderId":10}""");

        await Mediator.Received(1).Send(
            Arg.Is<FindPagesByTextQuery>(query =>
                query.Search.Pattern == new TextPattern("key", true, true)
                && query.Search.ScopeIds!.SequenceEqual(new[] { 11, 12 })
                && query.Search.Page == 1
                && query.Search.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindInPages_ShouldDescribeTheMatchingLines_OfEachPageTheDatabaseFound()
    {
        GivenFoundPages(total: 1, new PageBodyViewModel(13, "Welcome.\n\nRotate the api key yearly."));

        var result = await Execute(new FindInPagesTool(Mediator), """{"pattern":"API key"}""");

        using var content = JsonDocument.Parse(result.Content);
        var page = content.RootElement.GetProperty("pages").EnumerateArray().Single();
        var match = page.GetProperty("matches").EnumerateArray().Single();

        page.GetProperty("slug").GetString().Should().Be("handbook");
        page.GetProperty("matchCount").GetInt32().Should().Be(1);
        match.GetProperty("line").GetInt32().Should().Be(3);
        match.GetProperty("text").GetString().Should().Be("Rotate the api key yearly.");
    }

    [Fact]
    public async Task FindInPages_ShouldReportEveryMatchingPage_AndWhetherThereAreMore()
    {
        GivenFoundPages(total: 3, new PageBodyViewModel(11, "key"));

        var result = await Execute(new FindInPagesTool(Mediator), """{"pattern":"key","pageSize":1}""");

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("pagesMatched").GetInt32().Should().Be(3);
        content.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task FindInPages_ShouldRefuse_APatternTheDatabaseRejects()
    {
        Mediator
            .Send(Arg.Any<FindPagesByTextQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<PageBodyViewModel>>.Failed("The pattern is not a valid regular expression: x"));

        var result = await Execute(new FindInPagesTool(Mediator), """{"pattern":"x","regex":true}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("not a valid regular expression");
    }

    [Fact]
    public async Task FindInPages_ShouldRefuse_AnInvalidRegex_WithoutQuerying()
    {
        var result = await Execute(new FindInPagesTool(Mediator), """{"pattern":"(unclosed","regex":true}""");

        result.IsError.Should().BeTrue();
        await Mediator.DidNotReceive().Send(Arg.Any<FindPagesByTextQuery>(), Arg.Any<CancellationToken>());
    }

    private void GivenFoundPages(int total, params PageBodyViewModel[] pages)
    {
        var found = new PagedResponse<PageBodyViewModel>(pages, 1, 25, total);

        Mediator
            .Send(Arg.Any<FindPagesByTextQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<PageBodyViewModel>>.Success(found));
    }

    private void GivenPage(string slug, int id, string body)
    {
        var page = new PageViewModel { Id = id, Title = slug, Slug = slug, Kind = PageKind.Document, Body = body };

        Mediator
            .Send(Arg.Is<GetPageQuery>(query => query.Slug == slug), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PageViewModel>.Success(page));
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string arguments)
    {
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }
}
