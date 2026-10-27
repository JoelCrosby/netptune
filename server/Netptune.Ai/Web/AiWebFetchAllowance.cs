using System.Text.RegularExpressions;

namespace Netptune.Ai.Web;

// Which addresses the assistant may fetch in this turn. A model steered by something it read (a pull
// request, a web page, a repository file) could otherwise write private data into a URL and send it out,
// so only addresses that came from the user or from a web search are let through.
public sealed partial class AiWebFetchAllowance
{
    private readonly HashSet<string> Urls = new(StringComparer.Ordinal);
    private readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase);

    public void AllowFromUserText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (Match match in UrlPattern().Matches(text))
        {
            var candidate = match.Value.TrimEnd('.', ',', ';', ':', '!', '?');
            var isUrl = Uri.TryCreate(candidate, UriKind.Absolute, out var uri);

            if (!isUrl)
            {
                continue;
            }

            Urls.Add(Normalise(uri!));
            Hosts.Add(uri!.Host);
        }
    }

    public void AllowSearchResult(string? url)
    {
        var isUrl = Uri.TryCreate(url, UriKind.Absolute, out var uri);

        if (isUrl)
        {
            Urls.Add(Normalise(uri!));
        }
    }

    public bool IsAllowed(Uri uri)
    {
        return Hosts.Contains(uri.Host) || Urls.Contains(Normalise(uri));
    }

    private static string Normalise(Uri uri)
    {
        return uri.GetLeftPart(UriPartial.Query);
    }

    [GeneratedRegex(@"https?://[^\s<>""'`\)\]]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
