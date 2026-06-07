using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Services.Notifications;

using StackExchange.Redis;

namespace Netptune.App.Services;

[JsonSerializable(typeof(NotificationEvent))]
internal partial class NotificationEventSerializerContext : JsonSerializerContext;

public interface INotificationEventService
{
    Task SubscribeAsync(string userId, HttpResponse response, CancellationToken cancellationToken);

    Task PublishAsync(string userId, NotificationEvent notificationEvent);
}

public class NotificationEventService(
    ILogger<NotificationEventService> logger,
    IConnectionMultiplexer connection,
    INotificationEventPublisher publisher) : INotificationEventService
{
    private static RedisChannel ChannelForUser(string userId) =>
        RedisChannel.Literal($"notifications:{userId}");

    public async Task SubscribeAsync(string userId, HttpResponse response, CancellationToken cancellationToken)
    {
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var queue = await connection.GetSubscriber().SubscribeAsync(ChannelForUser(userId));

        try
        {
            await foreach (var message in queue)
            {
                try
                {
                    var notificationEvent = JsonSerializer.Deserialize(
                        message.Message.ToString(),
                        NotificationEventSerializerContext.Default.NotificationEvent);

                    if (notificationEvent is null) continue;

                    var json = JsonSerializer.Serialize(
                        notificationEvent,
                        NotificationEventSerializerContext.Default.NotificationEvent);

                    await response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Notification event deserialization failed");
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Notification SSE client disconnected");
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }

    public Task PublishAsync(string userId, NotificationEvent notificationEvent)
    {
        return publisher.PublishAsync(userId, notificationEvent);
    }
}
