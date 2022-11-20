using System;
using System.IO;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Netptune.Core.Services;
using Netptune.Core.Services.Integration;
using Netptune.Core.ViewModels.Web;

namespace Netptune.Services;

public class WebService : IWebService
{
    private readonly IHtmlDocumentService DocumentService;

    public WebService(IHtmlDocumentService documentService)
    {
        DocumentService = documentService;
    }

    public async Task<MetaInfo> GetMetaDataFromUrl(string url)
    {
        var formalUrl = GetFormalUrl(url);
        var document = await DocumentService.OpenAsync(formalUrl);

        var title = document.QuerySelector("title")?.TextContent;
        var metaTags = document.QuerySelectorAll("meta");

        var matchCount = 0;
        var metaInfo = new MetaInfo
        {
            Url = formalUrl,
            Title = title,
            SiteName = title,
        };

        foreach (var tag in metaTags)
        {
            var tagName = tag.Attributes["name"];
            var tagContent = tag.Attributes["content"];
            var tagProperty = tag.Attributes["property"];

            if (tagName is {} && tagContent is {})
            {
                switch (tagName.Value.ToLower())
                {
                    case "title":
                        metaInfo.Title = GetTagContent(metaInfo.Title, tagContent);
                        matchCount++;
                        break;
                    case "description":
                        metaInfo.Description = tagContent.Value;
                        matchCount++;
                        break;
                    case "twitter:title":
                        metaInfo.Title = GetTagContent(metaInfo.Title, tagContent);
                        matchCount++;
                        break;
                    case "twitter:description":
                        metaInfo.Description = GetTagContent(metaInfo.Description, tagContent);
                        matchCount++;
                        break;
                    case "keywords":
                        metaInfo.Keywords = tagContent.Value;
                        matchCount++;
                        break;
                    case "twitter:image":
                        metaInfo.Image.Url = GetImageTagContent(metaInfo.Image.Url, tagContent, formalUrl);
                        matchCount++;
                        break;
                }
            }
            else if (tagProperty is {} && tagContent is {})
            {
                switch (tagProperty.Value.ToLower())
                {
                    case "og:title":
                        metaInfo.Title = GetTagContent(metaInfo.Title, tagContent);
                        matchCount++;
                        break;
                    case "og:description":
                        metaInfo.Description = GetTagContent(metaInfo.Description, tagContent);
                        matchCount++;
                        break;
                    case "og:image":
                        metaInfo.Image.Url = GetImageTagContent(metaInfo.Image.Url, tagContent, formalUrl);
                        matchCount++;
                        break;
                }
            }
        }

        metaInfo.HasData = matchCount > 0;

        return metaInfo;
    }

    private static string GetImageTagContent(string value, IAttr tagContent, string formalUrl)
    {
        var tag = GetTagContent(value, tagContent);

        if (tag.StartsWith("https://") || tag.StartsWith("http://"))
        {
            return tag;
        }

        if (tag.StartsWith('.'))
        {
            return Path.Join(formalUrl, tag[1..]);
        }

        var uri = new Uri(formalUrl);
        var baseUrl = uri.GetLeftPart(UriPartial.Authority);

        return Path.Join(baseUrl, tag);
    }

    private static string GetTagContent(string? value, IAttr tagContent)
    {
        return string.IsNullOrEmpty(value) ? tagContent.Value : value;
    }

    private static string GetFormalUrl(string url)
    {
        var lower = url.ToLowerInvariant();

        if (lower.StartsWith("https://", StringComparison.Ordinal))
        {
            return lower;
        }

        if (lower.StartsWith("http://", StringComparison.Ordinal))
        {
            return lower;
        }

        return $"https://{lower}";
    }
}
