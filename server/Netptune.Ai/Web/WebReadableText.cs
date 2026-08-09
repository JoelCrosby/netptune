using System.Text;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Netptune.Ai.Web;

public sealed record WebReadableDocument
{
    public string? Title { get; init; }

    public string Text { get; init; } = string.Empty;
}

public static class WebReadableText
{
    private const string NoiseSelector =
        "script,style,noscript,template,svg,canvas,iframe,form,nav,header,footer,aside,"
        + "[role=navigation],[role=banner],[role=contentinfo],[aria-hidden=true]";

    private const string ContentSelector = "main,article,[role=main],#content,.content";

    private const string BlockSelector =
        "p,div,section,article,li,tr,pre,blockquote,figcaption,dt,dd,br,hr,h1,h2,h3,h4,h5,h6";

    private static readonly string[] HeadingTags = ["H1", "H2", "H3", "H4", "H5", "H6"];

    // Marks a block boundary while the text is still inside the DOM. Source newlines inside a
    // paragraph are ordinary whitespace to a browser, so only this survives as a line break.
    private const char BlockBreakChar = '\f';

    private const string BlockBreak = "\f";

    public static async Task<WebReadableDocument> Parse(string html, CancellationToken cancellationToken)
    {
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, cancellationToken);

        foreach (var noise in document.QuerySelectorAll(NoiseSelector))
        {
            noise.Remove();
        }

        var title = ReadTitle(document);
        var root = document.QuerySelector(ContentSelector) ?? document.Body;

        if (root is null)
        {
            return new WebReadableDocument { Title = title };
        }

        MarkBlocks(root);

        var text = Normalise(root.TextContent);

        return new WebReadableDocument { Title = title, Text = text };
    }

    private static string? ReadTitle(IDocument document)
    {
        var openGraph = document.QuerySelector("meta[property='og:title']")?.GetAttribute("content");
        var hasOpenGraph = !string.IsNullOrWhiteSpace(openGraph);

        if (hasOpenGraph)
        {
            return openGraph!.Trim();
        }

        var documentTitle = document.Title;
        var hasDocumentTitle = !string.IsNullOrWhiteSpace(documentTitle);

        if (hasDocumentTitle)
        {
            return documentTitle!.Trim();
        }

        return document.QuerySelector("h1")?.TextContent.Trim();
    }

    private static void MarkBlocks(IElement root)
    {
        foreach (var block in root.QuerySelectorAll(BlockSelector))
        {
            var isHeading = Array.IndexOf(HeadingTags, block.TagName) >= 0;

            if (isHeading)
            {
                var level = block.TagName[1] - '0';

                block.Insert(AdjacentPosition.BeforeBegin, $"{BlockBreak}{new string('#', level)} ");
            }

            var isListItem = block.TagName == "LI";

            if (isListItem)
            {
                block.Insert(AdjacentPosition.AfterBegin, "- ");
            }

            block.Insert(AdjacentPosition.AfterEnd, BlockBreak);
        }
    }

    private static string Normalise(string text)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        var lastWasSpace = false;

        foreach (var character in text)
        {
            var isBreak = character == BlockBreakChar;

            if (isBreak)
            {
                lines.Add(line.ToString().Trim());
                line.Clear();
                lastWasSpace = false;

                continue;
            }

            var isSpace = char.IsWhiteSpace(character);

            if (isSpace)
            {
                lastWasSpace = true;

                continue;
            }

            var needsSpace = lastWasSpace && line.Length > 0;

            if (needsSpace)
            {
                line.Append(' ');
            }

            line.Append(character);
            lastWasSpace = false;
        }

        lines.Add(line.ToString().Trim());

        return string.Join("\n", lines.Where(value => value.Length > 0));
    }

}
