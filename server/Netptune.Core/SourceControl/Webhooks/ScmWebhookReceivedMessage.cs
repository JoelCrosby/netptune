using Netptune.Core.Events;

namespace Netptune.Core.SourceControl.Webhooks;

public sealed record ScmWebhookReceivedMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.SourceControl;

    public long DeliveryRecordId { get; init; }
}
