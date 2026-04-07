namespace Netptune.App.Services;

public interface IBoardEventService
{
    Task SubscribeAsync(string group, HttpResponse response, CancellationToken cancellationToken);

    Task BroadcastAsync(string group);
}
