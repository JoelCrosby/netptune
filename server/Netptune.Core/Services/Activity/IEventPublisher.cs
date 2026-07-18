using Netptune.Core.Events;

namespace Netptune.Core.Services.Activity;

public interface IEventPublisher
{
    Task Dispatch<TPayload>(TPayload payload) where TPayload : class, IEventMessage;

    Task DispatchCanonical(CanonicalEventEnvelope envelope, CancellationToken cancellationToken = default);
}
