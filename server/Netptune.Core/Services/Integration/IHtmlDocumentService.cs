using System.Threading.Tasks;

using AngleSharp.Dom;

namespace Netptune.Core.Services.Integration;

public interface IHtmlDocumentService
{
    Task<IDocument> OpenAsync(string url);
}
