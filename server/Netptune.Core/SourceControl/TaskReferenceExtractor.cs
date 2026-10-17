using System.Text.RegularExpressions;

namespace Netptune.Core.SourceControl;

// Matches against the workspace's real project keys rather than a generic `letters-digits` shape:
// keys may contain hyphens (`proj-2`), and text like `utf-8` or `sha-256` must not look like a task.
public sealed class TaskReferenceExtractor
{
    private const int MaxReferences = 50;

    private readonly Regex? Pattern;

    public TaskReferenceExtractor(IEnumerable<string> projectKeys)
    {
        var alternatives = projectKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim().ToLowerInvariant())
            .Distinct()
            .OrderByDescending(key => key.Length)
            .Select(Regex.Escape)
            .ToList();

        if (alternatives.Count == 0)
        {
            return;
        }

        var keys = string.Join('|', alternatives);

        Pattern = new Regex(
            $@"(?<![\p{{L}}\p{{N}}])(?<key>{keys})-(?<number>\d{{1,9}})(?!\d)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));
    }

    public IReadOnlyList<string> Extract(params string?[] texts)
    {
        if (Pattern is null)
        {
            return [];
        }

        var found = new List<string>();

        foreach (var text in texts)
        {
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            foreach (Match match in Pattern.Matches(text))
            {
                var systemId = $"{match.Groups["key"].Value.ToLowerInvariant()}-{match.Groups["number"].Value}";
                var isNew = !found.Contains(systemId);

                if (isNew)
                {
                    found.Add(systemId);
                }

                if (found.Count == MaxReferences)
                {
                    return found;
                }
            }
        }

        return found;
    }
}
