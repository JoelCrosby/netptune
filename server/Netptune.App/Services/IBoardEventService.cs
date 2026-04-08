namespace Netptune.App.Services;

public interface IBoardEventService
{
    Task SubscribeAsync(string group, string clientId, HttpResponse response, CancellationToken cancellationToken);

    Task BroadcastAsync(string group, string clientId);
}
