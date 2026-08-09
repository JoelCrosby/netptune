using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Web;

public interface IWebSearchEngine
{
    WebSearchProvider Provider { get; }

    string? Validate(WebSearchCredential credential);

    HttpRequestMessage CreateRequest(WebSearchCredential credential, string query, int take);

    List<WebSearchHit> ReadHits(JsonElement root);
}

public sealed class BraveSearchEngine : IWebSearchEngine
{
    private const string DefaultEndpoint = "https://api.search.brave.com/res/v1/web/search";

    public WebSearchProvider Provider => WebSearchProvider.Brave;

    public string? Validate(WebSearchCredential credential)
    {
        var hasKey = !string.IsNullOrWhiteSpace(credential.ApiKey);

        return hasKey ? null : "Brave search needs an API key.";
    }

    public HttpRequestMessage CreateRequest(WebSearchCredential credential, string query, int take)
    {
        var endpoint = credential.Endpoint ?? DefaultEndpoint;
        var url = $"{endpoint}?q={Uri.EscapeDataString(query)}&count={take}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("X-Subscription-Token", credential.ApiKey);
        request.Headers.Add("Accept", "application/json");

        return request;
    }

    public List<WebSearchHit> ReadHits(JsonElement root)
    {
        var hasWeb = root.TryGetProperty("web", out var web);

        if (!hasWeb)
        {
            return [];
        }

        var hasResults = web.TryGetProperty("results", out var results);

        if (!hasResults)
        {
            return [];
        }

        return WebSearchJson.ReadHits(results, "url", "title", "description");
    }
}

public sealed class GoogleSearchEngine : IWebSearchEngine
{
    private const string DefaultEndpoint = "https://www.googleapis.com/customsearch/v1";

    public WebSearchProvider Provider => WebSearchProvider.Google;

    public string? Validate(WebSearchCredential credential)
    {
        var hasKey = !string.IsNullOrWhiteSpace(credential.ApiKey);
        var hasEngineId = !string.IsNullOrWhiteSpace(credential.EngineId);

        if (!hasKey)
        {
            return "Google search needs an API key.";
        }

        return hasEngineId ? null : "Google search needs a search engine id.";
    }

    public HttpRequestMessage CreateRequest(WebSearchCredential credential, string query, int take)
    {
        var endpoint = credential.Endpoint ?? DefaultEndpoint;
        var count = Math.Clamp(take, 1, 10);
        var url = $"{endpoint}?key={Uri.EscapeDataString(credential.ApiKey!)}"
            + $"&cx={Uri.EscapeDataString(credential.EngineId!)}"
            + $"&q={Uri.EscapeDataString(query)}"
            + $"&num={count}";

        return new HttpRequestMessage(HttpMethod.Get, url);
    }

    public List<WebSearchHit> ReadHits(JsonElement root)
    {
        var hasItems = root.TryGetProperty("items", out var items);

        if (!hasItems)
        {
            return [];
        }

        return WebSearchJson.ReadHits(items, "link", "title", "snippet");
    }
}

public sealed class SearxngSearchEngine : IWebSearchEngine
{
    public WebSearchProvider Provider => WebSearchProvider.Searxng;

    public string? Validate(WebSearchCredential credential)
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(credential.Endpoint);

        if (!hasEndpoint)
        {
            return "SearXNG needs the base URL of an instance.";
        }

        var isAbsolute = Uri.TryCreate(credential.Endpoint, UriKind.Absolute, out _);

        return isAbsolute ? null : "The SearXNG base URL is not a valid absolute URL.";
    }

    public HttpRequestMessage CreateRequest(WebSearchCredential credential, string query, int take)
    {
        var endpoint = credential.Endpoint!.TrimEnd('/');
        var url = $"{endpoint}/search?q={Uri.EscapeDataString(query)}&format=json";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("Accept", "application/json");

        var hasKey = !string.IsNullOrWhiteSpace(credential.ApiKey);

        if (hasKey)
        {
            request.Headers.Add("Authorization", $"Bearer {credential.ApiKey}");
        }

        return request;
    }

    public List<WebSearchHit> ReadHits(JsonElement root)
    {
        var hasResults = root.TryGetProperty("results", out var results);

        if (!hasResults)
        {
            return [];
        }

        return WebSearchJson.ReadHits(results, "url", "title", "content");
    }
}

public static class WebSearchJson
{
    public static List<WebSearchHit> ReadHits(JsonElement results, string urlName, string titleName, string snippetName)
    {
        var isArray = results.ValueKind == JsonValueKind.Array;

        if (!isArray)
        {
            return [];
        }

        var hits = new List<WebSearchHit>();

        foreach (var result in results.EnumerateArray())
        {
            var url = ReadString(result, urlName);
            var hasLink = !string.IsNullOrWhiteSpace(url);

            if (!hasLink)
            {
                continue;
            }

            hits.Add(new WebSearchHit
            {
                Title = ReadString(result, titleName) ?? url!,
                Url = url!,
                Snippet = ReadString(result, snippetName),
            });
        }

        return hits;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        var found = element.TryGetProperty(name, out var value);

        if (!found || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }
}
