using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;

namespace Netptune.SourceControl.Webhooks;

internal static class ScmPullRequestTransitions
{
    public static ScmPullRequestAction Derive(ScmPullRequestState? previous, ScmPullRequestState current)
    {
        var wasOpen = previous is ScmPullRequestState.Open or ScmPullRequestState.Draft;
        var wasMerged = previous == ScmPullRequestState.Merged;
        var wasFinished = previous is ScmPullRequestState.Merged or ScmPullRequestState.Closed;

        if (previous == current)
        {
            return ScmPullRequestAction.Edited;
        }

        return current switch
        {
            ScmPullRequestState.Merged => ScmPullRequestAction.Merged,
            ScmPullRequestState.Closed when !wasMerged => ScmPullRequestAction.Closed,
            ScmPullRequestState.Open or ScmPullRequestState.Draft when wasFinished => ScmPullRequestAction.Reopened,
            ScmPullRequestState.Open when wasOpen => ScmPullRequestAction.ReadyForReview,
            ScmPullRequestState.Draft when wasOpen => ScmPullRequestAction.ConvertedToDraft,
            _ => ScmPullRequestAction.Edited,
        };
    }
}
