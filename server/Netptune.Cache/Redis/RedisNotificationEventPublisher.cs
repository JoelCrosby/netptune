using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Services.Notifications;

using StackExchange.Redis;

namespace Netptune.Cache.Redis;

[JsonSerializable(typeof(NotificationEvent))]
internal partial class NotificationEventSerializerContext : JsonSerializerContext;

public sealed class RedisNotificationEventPublisher(IConnectionMultiplexer connection) : INotificationEventPublisher
{
    private const int PublishBatchSize = 500;

    public Task PublishAsync(
        string userId,
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = JsonSerializer.Serialize(
            notificationEvent,
            NotificationEventSerializerContext.Default.NotificationEvent);

        return connection.GetSubscriber().PublishAsync(ChannelForUser(userId), json);
    }

    public async Task PublishManyAsync(
        IEnumerable<UserNotificationEvent> notificationEvents,
        CancellationToken cancellationToken = default)
    {
        var subscriber = connection.GetSubscriber();

        foreach (var batch in notificationEvents.Chunk(PublishBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tasks = batch.Select(notification =>
            {
                var json = JsonSerializer.Serialize(
                    notification.Event,
                    NotificationEventSerializerContext.Default.NotificationEvent);

                return subscriber.PublishAsync(ChannelForUser(notification.UserId), json);
            });

            await Task.WhenAll(tasks);
        }
    }

    private static RedisChannel ChannelForUser(string userId) =>
        RedisChannel.Literal($"notifications:{userId}");
}
