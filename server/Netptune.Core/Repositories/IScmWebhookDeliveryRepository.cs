using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;

namespace Netptune.Core.Repositories;

public interface IScmWebhookDeliveryRepository : IRepository<ScmWebhookDelivery, long>
{
    Task<long?> AddIfNew(ScmWebhookDelivery delivery, CancellationToken cancellationToken = default);

    Task<List<long>> GetStalled(DateTime receivedBefore, int maxAttempts, int take, CancellationToken cancellationToken = default);

    Task<int> DeleteProcessedBefore(DateTime processedBefore, CancellationToken cancellationToken = default);

    Task<int> DeleteAbandonedBefore(DateTime receivedBefore, int maxAttempts, CancellationToken cancellationToken = default);

    Task<long?> ReviveUnprocessed(SourceControlProvider provider, string deliveryId, CancellationToken cancellationToken = default);

    Task RecordFailure(long id, string error, CancellationToken cancellationToken = default);
}
