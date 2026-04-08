using System.Text.Json;

using StackExchange.Redis;

namespace Netptune.App.Services;

public record ClientChannel(string Client, string Group, DateTimeOffset CreatedAt);

public class BoardEventService(ILogger<BoardEventService> logger, IConnectionMultiplexer connection) : IBoardEventService
{
    private readonly RedisChannel RealTimeGroupChannel = RedisChannel.Literal("real-time-groups");

    public async Task SubscribeAsync(
        string group,
        string clientId,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var queue = await connection.GetSubscriber().SubscribeAsync(RealTimeGroupChannel);

        try
        {
            await foreach (var message in queue)
            {
                try
                {
                    var clientChannel = JsonSerializer.Deserialize(message.Message.ToString(), BoardEventSerializerContext.Default.ClientChannel);

                    if (clientChannel is null) continue;

                    var notFromConnectedClient = clientChannel.Client != clientId;
                    var isSameGroup = clientChannel.Group == group;

                    if (notFromConnectedClient && isSameGroup)
                    {
                        await response.WriteAsync("data: update\n\n", cancellationToken);
                        await response.Body.FlushAsync(cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Message deserialization failed");
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Client disconnected");
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }

    public Task BroadcastAsync(string group, string clientId)
    {
        var message = new ClientChannel(clientId, group, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(message, BoardEventSerializerContext.Default.ClientChannel);

        return connection.GetSubscriber().PublishAsync(RealTimeGroupChannel, json);
    }
}
