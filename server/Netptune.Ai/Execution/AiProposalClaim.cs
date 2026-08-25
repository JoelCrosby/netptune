using System.Text.RegularExpressions;

namespace Netptune.Ai.Execution;

public static partial class AiProposalClaim
{
    public const string Correction =
        "Your reply says you proposed something, but no propose_ tool ran, so there is nothing for the user to review. "
        + "Call the propose_ tools the work needs now, then answer again saying what you proposed. "
        + "If you did not mean to propose anything, answer the user's message again as if this had not come up, "
        + "without mentioning proposals or the change set and without explaining the correction.";

    public static bool IsUnbacked(string text, int proposedCount)
    {
        var hasProposals = proposedCount > 0;
        var hasText = !string.IsNullOrWhiteSpace(text);

        if (hasProposals || !hasText)
        {
            return false;
        }

        var claimsToHaveProposed = FirstPersonProposal().IsMatch(text);
        var describesTheChangeSet = ChangeSetState().IsMatch(text);
        var claimsSomethingIsPending = PendingApproval().IsMatch(text);

        return claimsToHaveProposed || describesTheChangeSet || claimsSomethingIsPending;
    }

    // Only a completed claim counts. Matching the word "propose" anywhere caught the assistant
    // describing what it is able to do, and the correction turned those answers into a denial
    // that the user never asked for.
    [GeneratedRegex(@"\b(?:i|we)\b[^.!?]{0,20}\bproposed\b", RegexOptions.IgnoreCase)]
    private static partial Regex FirstPersonProposal();

    [GeneratedRegex(@"\bchange set\s+(?:now\s+|already\s+|still\s+)?(?:is|was|has|holds|contains|remains|awaits)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ChangeSetState();

    [GeneratedRegex(@"\b(?:awaits?|awaiting|pending|waiting for)\b[^.!?]{0,20}\bapproval\b", RegexOptions.IgnoreCase)]
    private static partial Regex PendingApproval();
}
