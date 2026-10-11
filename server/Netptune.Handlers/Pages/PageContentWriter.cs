using Netptune.Core.Models.Pages;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages;

public static class PageContentWriter
{
    // The revision-checked write and its version land together or not at all.
    public static Task<bool> TryWrite(INetptuneUnitOfWork unitOfWork, PageContentUpdate update, CancellationToken cancellationToken)
    {
        return unitOfWork.Transaction(async () =>
        {
            var saved = await unitOfWork.Pages.TrySaveContent(update, cancellationToken);

            if (!saved)
            {
                return false;
            }

            var snapshot = new PageVersionSnapshot
            {
                PageId = update.PageId,
                Revision = update.ExpectedRevision + 1,
                Title = update.Title,
                Body = update.Body,
                UserId = update.UserId,
                RestoredFromVersionId = update.RestoredFromVersionId,
            };

            await PageVersionRecorder.Record(unitOfWork, snapshot, cancellationToken);
            await PageLinkRebuilder.Rebuild(unitOfWork, update, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return true;
        });
    }
}
