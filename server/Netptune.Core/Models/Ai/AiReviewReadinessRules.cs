using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Models.Ai;

public static class AiReviewReadinessRules
{
    public static AiReviewReadiness For(bool isStale, int openCount)
    {
        if (isStale)
        {
            return AiReviewReadiness.Stale;
        }

        var hasOpenFindings = openCount > 0;

        return hasOpenFindings ? AiReviewReadiness.NeedsWork : AiReviewReadiness.Ready;
    }
}
