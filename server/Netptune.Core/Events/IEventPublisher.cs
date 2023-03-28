using System.Threading.Tasks;

namespace Netptune.Core.Events;

public interface IEventPublisher
{
    Task Dispatch<TPayload>(NetptuneEvent type, TPayload payload) where TPayload : class;
}
