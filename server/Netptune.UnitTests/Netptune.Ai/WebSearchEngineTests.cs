using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Web;
using Netptune.Core.Enums;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class WebSearchEngineTests
{
    [Fact]
    public void Brave_ShouldRequireAnApiKey()
    {
        var engine = new BraveSearchEngine();

        engine.Validate(new WebSearchCredential()).Should().NotBeNull();
        engine.Validate(new WebSearchCredential { ApiKey = "token" }).Should().BeNull();
    }

    [Fact]
    public void Brave_ShouldSendTheKeyAsASubscriptionHeader()
    {
        var engine = new BraveSearchEngine();
        var credential = new WebSearchCredential { Provider = WebSearchProvider.Brave, ApiKey = "token" };

        using var request = engine.CreateRequest(credential, "netptune docs", 5);

        request.Headers.GetValues("X-Subscription-Token").Should().Equal("token");
        request.RequestUri!.Query.Should().Contain("q=netptune%20docs").And.Contain("count=5");
    }

    [Fact]
    public void Google_ShouldRequireBothAKeyAndAnEngineId()
    {
        var engine = new GoogleSearchEngine();

        engine.Validate(new WebSearchCredential { ApiKey = "token" }).Should().NotBeNull();
        engine.Validate(new WebSearchCredential { EngineId = "cx" }).Should().NotBeNull();
        engine.Validate(new WebSearchCredential { ApiKey = "token", EngineId = "cx" }).Should().BeNull();
    }

    [Fact]
    public void Google_ShouldCapTheResultCountAtTheApiLimit()
    {
        var engine = new GoogleSearchEngine();
        var credential = new WebSearchCredential { ApiKey = "token", EngineId = "cx" };

        using var request = engine.CreateRequest(credential, "netptune", 50);

        request.RequestUri!.Query.Should().Contain("num=10", "the Google API rejects more than ten per page");
    }

    [Fact]
    public void Searxng_ShouldRequireAnAbsoluteEndpoint()
    {
        var engine = new SearxngSearchEngine();

        engine.Validate(new WebSearchCredential()).Should().NotBeNull();
        engine.Validate(new WebSearchCredential { Endpoint = "searxng" }).Should().NotBeNull();
        engine.Validate(new WebSearchCredential { Endpoint = "http://searxng:8080" }).Should().BeNull();
    }

    [Fact]
    public void Searxng_ShouldAskForJsonAndNotDoubleTheSlash()
    {
        var engine = new SearxngSearchEngine();
        var credential = new WebSearchCredential { Endpoint = "http://searxng:8080/" };

        using var request = engine.CreateRequest(credential, "netptune", 5);

        request.RequestUri!.ToString().Should().StartWith("http://searxng:8080/search?");
        request.RequestUri.Query.Should().Contain("format=json");
    }

    [Fact]
    public void Brave_ShouldReadHitsFromTheWebResults()
    {
        var engine = new BraveSearchEngine();

        using var payload = JsonDocument.Parse(
            """
            {"web":{"results":[
              {"url":"https://example.com/a","title":"A","description":"first"},
              {"title":"No link"}
            ]}}
            """);

        var hits = engine.ReadHits(payload.RootElement);

        hits.Should().ContainSingle();
        hits[0].Url.Should().Be("https://example.com/a");
        hits[0].Snippet.Should().Be("first");
    }

    [Fact]
    public void Google_ShouldReadHitsFromItems()
    {
        var engine = new GoogleSearchEngine();

        using var payload = JsonDocument.Parse(
            """{"items":[{"link":"https://example.com/b","title":"B","snippet":"second"}]}""");

        var hits = engine.ReadHits(payload.RootElement);

        hits.Should().ContainSingle();
        hits[0].Url.Should().Be("https://example.com/b");
        hits[0].Snippet.Should().Be("second");
    }

    [Fact]
    public void Searxng_ShouldReadHitsFromResults()
    {
        var engine = new SearxngSearchEngine();

        using var payload = JsonDocument.Parse(
            """{"results":[{"url":"https://example.com/c","title":"C","content":"third"}]}""");

        var hits = engine.ReadHits(payload.RootElement);

        hits.Should().ContainSingle();
        hits[0].Url.Should().Be("https://example.com/c");
        hits[0].Snippet.Should().Be("third");
    }

    [Fact]
    public void EveryEngine_ShouldReturnNothing_WhenThePayloadHasNoResults()
    {
        using var payload = JsonDocument.Parse("""{"unexpected":true}""");

        new BraveSearchEngine().ReadHits(payload.RootElement).Should().BeEmpty();
        new GoogleSearchEngine().ReadHits(payload.RootElement).Should().BeEmpty();
        new SearxngSearchEngine().ReadHits(payload.RootElement).Should().BeEmpty();
    }
}
