namespace Netptune.Core.Events;

public interface ICanonicalEventConsumer
{
    static abstract string DurableName { get; }

    static abstract bool ReplaysHistory { get; }

    ValueTask Handle(CanonicalEventEnvelope envelope, CancellationToken cancellationToken);
}
