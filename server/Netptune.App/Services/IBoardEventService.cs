using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Netptune.App.Services;

public interface IBoardEventService
{
    Task SubscribeAsync(string group, HttpResponse response, CancellationToken cancellationToken);

    Task BroadcastAsync(string group);
}
