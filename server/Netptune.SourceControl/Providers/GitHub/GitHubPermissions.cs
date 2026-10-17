using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Providers.GitHub;

internal static class GitHubPermissions
{
    public static SourceControlCapabilities ToCapabilities(IReadOnlyDictionary<string, string>? permissions)
    {
        var granted = permissions ?? new Dictionary<string, string>();

        var canReadMetadata = CanRead(granted, "metadata");
        var canReadContents = CanRead(granted, "contents");
        var canReadPullRequests = CanRead(granted, "pull_requests");
        var canReadChecks = CanRead(granted, "checks");

        var capabilities = SourceControlCapabilities.Webhooks;

        if (canReadMetadata)
        {
            capabilities |= SourceControlCapabilities.ListRepositories;
        }

        if (canReadContents)
        {
            capabilities |= SourceControlCapabilities.ReadBranches | SourceControlCapabilities.ReadCommits;
        }

        if (canReadPullRequests)
        {
            capabilities |= SourceControlCapabilities.ReadPullRequests;
        }

        if (canReadChecks)
        {
            capabilities |= SourceControlCapabilities.ReadCheckRuns;
        }

        return capabilities;
    }

    private static bool CanRead(IReadOnlyDictionary<string, string> permissions, string name)
    {
        var found = permissions.TryGetValue(name, out var level);

        return found && level is "read" or "write" or "admin";
    }
}
