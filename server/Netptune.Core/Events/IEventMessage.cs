using Mediator;

namespace Netptune.Core.Events;

public interface IEventMessage : IRequest
{
    static abstract string Subject { get; }
}
