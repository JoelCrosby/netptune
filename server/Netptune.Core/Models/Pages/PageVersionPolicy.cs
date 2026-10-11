using Netptune.Core.Entities;

namespace Netptune.Core.Models.Pages;

public static class PageVersionPolicy
{
    public static readonly TimeSpan CoalesceWindow = TimeSpan.FromMinutes(10);

    public static bool ShouldCoalesce(PageVersion? latest, string userId, DateTime now)
    {
        if (latest is null)
        {
            return false;
        }

        var sameAuthor = latest.CreatedByUserId == userId;
        var isRestorePoint = latest.RestoredFromVersionId.HasValue;
        var isFirstVersion = latest.Revision <= 1;
        var withinWindow = now - latest.CreatedAt < CoalesceWindow;

        return sameAuthor && withinWindow && !isRestorePoint && !isFirstVersion;
    }
}
