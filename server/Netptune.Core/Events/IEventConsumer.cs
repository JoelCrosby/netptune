using System.Collections.Generic;
using System.Threading;

namespace Netptune.Core.Events;

public interface IEventConsumer
{
    IAsyncEnumerable<EventMessage> GetEventMessages(CancellationToken cancellationToken);
}
