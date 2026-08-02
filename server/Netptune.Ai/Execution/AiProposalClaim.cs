namespace Netptune.Ai.Execution;

public static class AiProposalClaim
{
    public const string Correction =
        "Your reply describes a proposal, but no propose_ tool ran this turn, so the change set is empty "
        + "and the user has nothing to review or apply. Saying it without calling the tool changes nothing. "
        + "Call the propose_ tools the work needs now, then answer again describing what you proposed. "
        + "If you did not mean to propose anything, answer again without saying that you did.";

    private static readonly string[] ClaimMarkers =
    [
        "propos",
        "change set",
        "review and apply",
        "awaiting your approval",
        "for you to review",
    ];

    public static bool IsUnbacked(string text, int proposedCount)
    {
        var hasProposals = proposedCount > 0;
        var hasText = !string.IsNullOrWhiteSpace(text);

        if (hasProposals || !hasText)
        {
            return false;
        }

        return ClaimMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
