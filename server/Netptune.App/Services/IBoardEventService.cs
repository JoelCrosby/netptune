namespace Netptune.App.Services;

public interface IBoardEventService
{
    Task SubscribeAsync(
        RealtimeSubscription subscription,
        HttpResponse response,
        CancellationToken cancellationToken);

    Task BroadcastAsync(string workspace, string sourceClientId);
}
