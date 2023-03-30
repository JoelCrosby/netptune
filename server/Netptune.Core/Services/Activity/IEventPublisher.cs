using System.Threading.Tasks;

namespace Netptune.Core.Services.Activity;

public interface IEventPublisher
{
    Task Dispatch<TPayload>(TPayload payload) where TPayload : class;
}
