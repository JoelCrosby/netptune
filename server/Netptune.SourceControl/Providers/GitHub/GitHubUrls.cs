using Netptune.Core.SourceControl.Models;

namespace Netptune.SourceControl.Providers.GitHub;

internal static class GitHubUrls
{
    public static string Repository(ScmRepositoryRef repository)
    {
        var owner = Uri.EscapeDataString(repository.Owner);
        var name = Uri.EscapeDataString(repository.Name);

        return $"/repos/{owner}/{name}";
    }

    public static string Branch(string webBaseUrl, ScmRepositoryRef repository, string branch)
    {
        var owner = Uri.EscapeDataString(repository.Owner);
        var name = Uri.EscapeDataString(repository.Name);
        var branchPath = EscapePath(branch);

        return $"{webBaseUrl.TrimEnd('/')}/{owner}/{name}/tree/{branchPath}";
    }

    public static string EscapePath(string path)
    {
        var rawSegments = path.Split('/');
        var hasDotSegment = rawSegments.Any(segment => segment is "." or "..");

        if (hasDotSegment)
        {
            throw new ArgumentException("A path must not contain \".\" or \"..\" segments.", nameof(path));
        }

        var segments = rawSegments.Select(Uri.EscapeDataString);

        return string.Join('/', segments);
    }
}
