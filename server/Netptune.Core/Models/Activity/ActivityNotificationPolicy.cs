using Netptune.Core.Enums;

namespace Netptune.Core.Models.Activity;

public static class ActivityNotificationPolicy
{
    private static readonly HashSet<ActivityType> NeverNotified =
    [
        ActivityType.ExportRequested,
        ActivityType.LoginSuccess,
        ActivityType.LoginFailed,
        ActivityType.PullRequestOpened,
        ActivityType.PullRequestMerged,
        ActivityType.PullRequestClosed,
        ActivityType.CommitsPushed,
        ActivityType.ChecksPassed,
        ActivityType.ChecksFailed,
        ActivityType.Enable,
        ActivityType.Disable,
        ActivityType.RunReview,
        ActivityType.AcceptReviewFinding,
        ActivityType.DismissReviewFinding,
        ActivityType.ReopenReviewFinding,
    ];

    public static bool IsNotifiable(ActivityType activityType)
    {
        return !NeverNotified.Contains(activityType);
    }
}
