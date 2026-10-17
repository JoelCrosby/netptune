using Netptune.Core.BaseEntities;
using Netptune.Core.SourceControl;

namespace Netptune.Core.Entities;

public sealed record ScmWebhookDelivery : KeyedEntity<long>
{
    public SourceControlProvider Provider { get; init; }

    public required string DeliveryId { get; init; }

    public required string EventName { get; init; }

    public string? Action { get; init; }

    public string? ExternalAccountId { get; init; }

    public required string Body { get; init; }

    public DateTime ReceivedAt { get; init; }

    public DateTime? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
