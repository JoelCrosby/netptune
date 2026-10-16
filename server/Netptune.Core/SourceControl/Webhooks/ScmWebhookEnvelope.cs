namespace Netptune.Core.SourceControl.Webhooks;

public sealed record ScmWebhookEnvelope
{
    public SourceControlProvider Provider { get; init; }

    public required string DeliveryId { get; init; }

    public required string EventName { get; init; }

    public string? Action { get; init; }

    public string? ExternalAccountId { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }

    public required ReadOnlyMemory<byte> Body { get; init; }
}
