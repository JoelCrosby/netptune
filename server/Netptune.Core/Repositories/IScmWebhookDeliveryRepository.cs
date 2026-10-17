using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IScmWebhookDeliveryRepository : IRepository<ScmWebhookDelivery, long>
{
    Task<long?> AddIfNew(ScmWebhookDelivery delivery, CancellationToken cancellationToken = default);

    Task<List<long>> GetStalled(DateTime receivedBefore, int maxAttempts, int take, CancellationToken cancellationToken = default);

    Task<int> DeleteProcessedBefore(DateTime processedBefore, CancellationToken cancellationToken = default);
}
