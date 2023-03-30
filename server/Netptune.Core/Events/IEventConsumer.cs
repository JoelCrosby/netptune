using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Events;

public interface IEventConsumer
{
    Task Connect();

    Task<IEnumerable<EventMessage>> GetEventMessages();
}
