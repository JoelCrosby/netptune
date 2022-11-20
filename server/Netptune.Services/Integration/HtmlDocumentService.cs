using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;

using Netptune.Core.Services.Integration;

namespace Netptune.Services.Integration;

public class HtmlDocumentService : IHtmlDocumentService
{
    public Task<IDocument> OpenAsync(string url)
    {
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);

        return context.OpenAsync(url);
    }
}
