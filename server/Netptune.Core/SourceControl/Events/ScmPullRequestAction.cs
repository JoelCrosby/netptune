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
}
