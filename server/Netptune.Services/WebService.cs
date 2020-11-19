using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Web;

namespace Netptune.Services
{
    public class WebService : IWebService
    {
        public async Task<MetaInfo> GetMetaDataFromUrl(string url)
        {
            var formalUrl = GetFormalUrl(url);

            var config = AngleSharp.Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(formalUrl);

            if (document is null)
            {
                return new MetaInfo();
            }

            var title = document.QuerySelector("title")?.TextContent;
            var metaTags = document.QuerySelectorAll("meta");

            if (metaTags is null)
            {
                return new MetaInfo();
            }

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
                            metaInfo.Image.Url = GetTagContent(metaInfo.Image.Url, tagContent);
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
                            metaInfo.Image.Url = GetTagContent(metaInfo.Image.Url, tagContent);
                            matchCount++;
                            break;
                    }
                }
            }

            metaInfo.HasData = matchCount > 0;

            return metaInfo;
        }

        private static string GetTagContent(string value, IAttr tagContent)
        {
            return string.IsNullOrEmpty(value) ? tagContent.Value : value;
        }

        private static string GetFormalUrl(string url)
        {
            var lower = url.ToLowerInvariant();

            if (lower.StartsWith("https://"))
            {
                return lower;
            }

            if (lower.StartsWith("http://"))
            {
                return $"https://{lower.Substring(6, lower.Length - 6)}";
            }

            return $"https://{lower}";
        }
    }
}
