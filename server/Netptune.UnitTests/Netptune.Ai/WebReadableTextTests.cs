using FluentAssertions;

using Netptune.Ai.Web;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class WebReadableTextTests
{
    [Fact]
    public async Task ShouldDropChromeAndScripts()
    {
        const string html = """
            <html>
              <head><title>Ship it</title></head>
              <body>
                <nav>Home Products Pricing</nav>
                <script>window.tracking = 1;</script>
                <style>body { color: red; }</style>
                <main><p>The release went out on Tuesday.</p></main>
                <footer>Copyright 2026</footer>
              </body>
            </html>
            """;

        var document = await WebReadableText.Parse(html, TestContext.Current.CancellationToken);

        document.Text.Should().Contain("The release went out on Tuesday.");
        document.Text.Should().NotContain("tracking");
        document.Text.Should().NotContain("color: red");
        document.Text.Should().NotContain("Pricing");
        document.Text.Should().NotContain("Copyright");
    }

    [Fact]
    public async Task ShouldPreferTheMainContentRegion()
    {
        const string html = """
            <html><body>
              <div class="sidebar">Related reading</div>
              <article><p>Only the article body.</p></article>
            </body></html>
            """;

        var document = await WebReadableText.Parse(html, TestContext.Current.CancellationToken);

        document.Text.Should().Be("Only the article body.");
    }

    [Fact]
    public async Task ShouldKeepHeadingsAndListsAsMarkdown()
    {
        const string html = """
            <html><body><main>
              <h2>Requirements</h2>
              <ul><li>Postgres</li><li>Redis</li></ul>
            </main></body></html>
            """;

        var document = await WebReadableText.Parse(html, TestContext.Current.CancellationToken);

        document.Text.Should().Contain("## Requirements");
        document.Text.Should().Contain("- Postgres");
        document.Text.Should().Contain("- Redis");
    }

    [Fact]
    public async Task ShouldReadTheOpenGraphTitleFirst()
    {
        const string html = """
            <html>
              <head>
                <title>Site name — page</title>
                <meta property="og:title" content="The real title" />
              </head>
              <body><p>Body</p></body>
            </html>
            """;

        var document = await WebReadableText.Parse(html, TestContext.Current.CancellationToken);

        document.Title.Should().Be("The real title");
    }

    [Fact]
    public async Task ShouldCollapseWhitespaceRuns()
    {
        const string html = "<html><body><main><p>One     two\n\n\n   three</p></main></body></html>";

        var document = await WebReadableText.Parse(html, TestContext.Current.CancellationToken);

        document.Text.Should().Be("One two three");
    }
}
