using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public static class AutomationSourceControlTrigger
{
    public const int MaxTargetBranches = 20;

    private const int MaxBranchLength = 255;

    public static bool IsPullRequest(AutomationTriggerType type)
    {
        return type is AutomationTriggerType.PullRequestOpened or AutomationTriggerType.PullRequestMerged;
    }

    public static bool IsValidBranchPattern(string? pattern)
    {
        var trimmed = pattern?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed == "*")
        {
            return false;
        }

        var wildcardIndex = trimmed.IndexOf('*');
        var wildcardIsTrailing = wildcardIndex == -1 || wildcardIndex == trimmed.Length - 1;
        var hasWhitespace = trimmed.Any(char.IsWhiteSpace);
        var hasValidLength = trimmed.Length <= MaxBranchLength;

        return wildcardIsTrailing && !hasWhitespace && hasValidLength;
    }

    // Branch names are case-sensitive in git, so the match is too.
    public static bool MatchesBranch(IReadOnlyCollection<string> patterns, string? branch)
    {
        if (patterns.Count == 0)
        {
            return true;
        }

        if (branch is null)
        {
            return false;
        }

        return patterns.Any(pattern => MatchesPattern(pattern.Trim(), branch));
    }

    private static bool MatchesPattern(string pattern, string branch)
    {
        var isPrefix = pattern.EndsWith('*');

        return isPrefix
            ? branch.StartsWith(pattern[..^1], StringComparison.Ordinal)
            : string.Equals(pattern, branch, StringComparison.Ordinal);
    }
}
