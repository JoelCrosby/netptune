namespace Netptune.Core.Events;

public interface IEventConsumer
{
    IEnumerable<EventMessage> GetEventMessages(CancellationToken cancellationToken);
}
