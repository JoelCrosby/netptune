namespace Netptune.Core.Models.Pages;

public sealed record PageReviewWindow
{
    public const int StaleAfterDays = 180;
    public const int UnvisitedAfterDays = 90;

    public int WorkspaceId { get; init; }

    public DateTime StaleBefore { get; init; }

    public DateTime UnvisitedBefore { get; init; }

    public int Take { get; init; }

    public static PageReviewWindow At(int workspaceId, DateTime now, int take)
    {
        return new PageReviewWindow
        {
            WorkspaceId = workspaceId,
            StaleBefore = now.AddDays(-StaleAfterDays),
            UnvisitedBefore = now.AddDays(-UnvisitedAfterDays),
            Take = take,
        };
    }
}
