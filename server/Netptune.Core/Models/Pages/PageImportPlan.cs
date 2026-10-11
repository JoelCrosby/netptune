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
        var folders = new Dictionary<string, PageImportNode>(StringComparer.OrdinalIgnoreCase);
        var documents = new List<PageImportNode>();

        foreach (var file in files.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase))
        {
            var segments = Segments(file.Path);

            if (segments.Count == 0)
            {
                continue;
            }

            var parentKey = AddFolders(folders, segments[..^1]);
            var (title, body) = ReadTitle(file.Content, segments[^1]);

            documents.Add(new PageImportNode
            {
                Key = string.Join('/', segments),
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

    private static string? AddFolders(Dictionary<string, PageImportNode> folders, List<string> directories)
    {
        string? parentKey = null;

        for (var depth = 0; depth < directories.Count; depth++)
        {
            var key = string.Join('/', directories.Take(depth + 1));

            folders.TryAdd(key, new PageImportNode
            {
                Key = key,
                ParentKey = parentKey,
                Kind = PageKind.Folder,
                Title = Fit(directories[depth]),
            });

            parentKey = key;
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

    [GeneratedRegex(@"\A\s*#[ \t]+(?<title>[^\n]+?)[ \t#]*(\n|\z)")]
    private static partial Regex LeadingHeadingPattern();
}
