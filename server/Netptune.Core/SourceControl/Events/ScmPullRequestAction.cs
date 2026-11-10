namespace Netptune.Core.SourceControl.Events;

public enum ScmPullRequestAction
{
    Opened = 0,
    Edited = 1,
    Synchronized = 2,
    ReadyForReview = 3,
    ConvertedToDraft = 4,
    Merged = 5,
    Closed = 6,
    Reopened = 7,

    // The provider didn't say what changed, so the action is worked out from the stored link state.
    Updated = 8,
}
