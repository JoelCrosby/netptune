namespace Netptune.Core.SourceControl.Webhooks;

public interface IScmWebhookProcessor
{
    Task Process(long deliveryRecordId, CancellationToken cancellationToken);
}
