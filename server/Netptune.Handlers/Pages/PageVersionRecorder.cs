using Netptune.Core.Entities;
using Netptune.Core.Models.Pages;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages;

public static class PageVersionRecorder
{
    // Stages the version on the unit of work; the caller's CompleteAsync writes it.
    public static async Task Record(
        INetptuneUnitOfWork unitOfWork,
        PageVersionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var latest = await unitOfWork.PageVersions.GetLatest(snapshot.PageId, cancellationToken);
        var isRestore = snapshot.RestoredFromVersionId.HasValue;
        var coalesce = !isRestore && PageVersionPolicy.ShouldCoalesce(latest, snapshot.UserId, now);

        if (coalesce)
        {
            latest!.Revision = snapshot.Revision;
            latest.Title = snapshot.Title;
            latest.Body = snapshot.Body;
            latest.UpdatedAt = now;

            return;
        }

        var version = ToVersion(snapshot, now);

        await unitOfWork.PageVersions.AddAsync(version, cancellationToken);
    }

    public static PageVersion ToVersion(PageVersionSnapshot snapshot, DateTime now)
    {
        return new PageVersion
        {
            PageId = snapshot.PageId,
            Revision = snapshot.Revision,
            Title = snapshot.Title,
            Body = snapshot.Body,
            CreatedAt = now,
            CreatedByUserId = snapshot.UserId,
            RestoredFromVersionId = snapshot.RestoredFromVersionId,
        };
    }
}
