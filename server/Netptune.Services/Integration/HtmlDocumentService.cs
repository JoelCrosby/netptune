using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Netptune.Core.Exceptions;
using Netptune.Core.Services.Integration;

namespace Netptune.Services.Integration;

public class HtmlDocumentService(HttpClient client) : IHtmlDocumentService
{
    private readonly HtmlParser Parser = new();

    public async Task<IDocument> OpenAsync(string url)
    {
        var uri = UrlEgressBlockedException.ThrowIfNotAbsoluteUrl(url);

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

        UrlEgressBlockedException.ThrowIfNotSuccess(response);

        await using var stream = await response.Content.ReadAsStreamAsync();

        return await Parser.ParseDocumentAsync(stream);
    }
}
