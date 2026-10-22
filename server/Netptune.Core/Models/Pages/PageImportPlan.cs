using System.Text.RegularExpressions;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Pages;

public sealed record PageImportFile(string Path, string Content);

public sealed record PageImportNode
{
    public required string Key { get; init; }

    public string? ParentKey { get; init; }

    public PageKind Kind { get; init; }

    public required string Title { get; init; }

    public string? Body { get; init; }
}

public static partial class PageImportPlan
{
    public static List<PageImportNode> Build(IReadOnlyList<PageImportFile> files)
    {
        var keys = new UniqueKeys();
        var folders = new Dictionary<string, PageImportNode>(StringComparer.OrdinalIgnoreCase);
        var documents = new List<PageImportNode>();

        foreach (var file in files.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase))
        {
            var segments = Segments(file.Path);

            if (segments.Count == 0)
            {
                continue;
            }

            var parentKey = AddFolders(folders, keys, segments[..^1]);
            var (title, body) = ReadTitle(file.Content, segments[^1]);
            var path = string.Join('/', segments);

            documents.Add(new PageImportNode
            {
                Key = keys.Claim(path),
                ParentKey = parentKey,
                Kind = PageKind.Document,
                Title = title,
                Body = body,
            });
        }

        return [.. folders.Values, .. documents];
    }

    public static List<string> Segments(string path)
    {
        return path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => segment is not "." and not "..")
            .ToList();
    }

    private static string? AddFolders(Dictionary<string, PageImportNode> folders, UniqueKeys keys, List<string> directories)
    {
        string? parentKey = null;

        for (var depth = 0; depth < directories.Count; depth++)
        {
            var path = string.Join('/', directories.Take(depth + 1));

            if (!folders.TryGetValue(path, out var folder))
            {
                folder = new PageImportNode
                {
                    Key = keys.Claim(path),
                    ParentKey = parentKey,
                    Kind = PageKind.Folder,
                    Title = Fit(directories[depth]),
                };

                folders.Add(path, folder);
            }

            parentKey = folder.Key;
        }

        return parentKey;
    }

    private static (string Title, string Body) ReadTitle(string content, string fileName)
    {
        var normalised = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var heading = LeadingHeadingPattern().Match(normalised);

        if (heading.Success)
        {
            var title = Fit(heading.Groups["title"].Value);
            var body = normalised[(heading.Index + heading.Length)..].Trim('\n');

            return (title, body);
        }

        var fallback = Fit(Path.GetFileNameWithoutExtension(fileName));

        return (fallback, normalised.Trim('\n'));
    }

    private static string Fit(string title)
    {
        var normalised = PageTitle.Normalise(title);

        return PageTitle.IsTooLong(normalised) ? normalised[..PageTitle.MaxLength] : normalised;
    }

    private sealed class UniqueKeys
    {
        private readonly HashSet<string> Taken = new(StringComparer.OrdinalIgnoreCase);

        public string Claim(string path)
        {
            var key = path;

            for (var copy = 2; !Taken.Add(key); copy++)
            {
                key = $"{path} ({copy})";
            }

            return key;
        }
    }

    [GeneratedRegex(@"\A\s*#[ \t]+(?<title>[^\n]+?)(?:[ \t]+#+)?[ \t]*(\n|\z)")]
    private static partial Regex LeadingHeadingPattern();
}
