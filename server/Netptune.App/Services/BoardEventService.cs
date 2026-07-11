using System.Text.Json;
using System.Threading.Channels;

using StackExchange.Redis;

namespace Netptune.App.Services;

public record ClientChannel(string Client, string Group, DateTimeOffset CreatedAt);

public enum PresenceKind
{
    Join,
    Hello,
    Leave,
}

public record PresenceMessage(string Group, string UserId, string ClientId, PresenceKind Kind);

public class BoardEventService(ILogger<BoardEventService> logger, IConnectionMultiplexer connection) : IBoardEventService
{
    private readonly RedisChannel RealTimeGroupChannel = RedisChannel.Literal("real-time-groups");
    private readonly RedisChannel PresenceChannel = RedisChannel.Literal("board-presence");

    public async Task SubscribeAsync(
        string group,
        string clientId,
        string userId,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var subscriber = connection.GetSubscriber();
        var reloadQueue = await subscriber.SubscribeAsync(RealTimeGroupChannel);
        var presenceQueue = await subscriber.SubscribeAsync(PresenceChannel);

        // Every SSE frame is written by the single consumer loop at the bottom of this method.
        // The reload and presence handlers only enqueue frames, so they never touch the response
        // concurrently.
        var outbound = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

        // Per-connection view of who is online in this group, keyed by userId. Only ever touched
        // from the presence handler, which Redis invokes sequentially for this queue.
        var present = new Dictionary<string, HashSet<string>> { [userId] = [clientId] };

        reloadQueue.OnMessage(message =>
        {
            try
            {
                var clientChannel = JsonSerializer.Deserialize(message.Message.ToString(), BoardEventSerializerContext.Default.ClientChannel);

                if (clientChannel is null) return;

                var notFromConnectedClient = clientChannel.Client != clientId;
                var isSameGroup = clientChannel.Group == group;

                if (notFromConnectedClient && isSameGroup)
                {
                    outbound.Writer.TryWrite("data: update\n\n");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Message deserialization failed");
            }
        });

        // Send the initial snapshot (just this client) before the presence handler can mutate the map.
        outbound.Writer.TryWrite(PresenceFrame(present));

        presenceQueue.OnMessage(message =>
        {
            try
            {
                var presence = JsonSerializer.Deserialize(message.Message.ToString(), BoardEventSerializerContext.Default.PresenceMessage);

                if (presence is null || presence.Group != group || presence.ClientId == clientId) return;

                var changed = presence.Kind switch
                {
                    PresenceKind.Join => AddPresence(present, presence.UserId, presence.ClientId),
                    PresenceKind.Hello => AddPresence(present, presence.UserId, presence.ClientId),
                    PresenceKind.Leave => RemovePresence(present, presence.UserId, presence.ClientId),
                    _ => false,
                };

                // Reply to joins so the newcomer learns we are here. Hello replies do not trigger further replies.
                if (presence.Kind is PresenceKind.Join)
                {
                    _ = BroadcastPresenceAsync(group, userId, clientId, PresenceKind.Hello);
                }

                if (changed)
                {
                    outbound.Writer.TryWrite(PresenceFrame(present));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Presence message handling failed");
            }
        });

        // Announce our arrival so existing viewers reply with their presence.
        await BroadcastPresenceAsync(group, userId, clientId, PresenceKind.Join);

        try
        {
            await foreach (var frame in outbound.Reader.ReadAllAsync(cancellationToken))
            {
                await response.WriteAsync(frame, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Client disconnected");
        }
        finally
        {
            await BroadcastPresenceAsync(group, userId, clientId, PresenceKind.Leave);
            await reloadQueue.UnsubscribeAsync();
            await presenceQueue.UnsubscribeAsync();
        }
    }

    private static string PresenceFrame(Dictionary<string, HashSet<string>> present)
    {
        var users = present.Keys.ToArray();
        var json = JsonSerializer.Serialize(users, BoardEventSerializerContext.Default.StringArray);

        return $"event: presence\ndata: {json}\n\n";
    }

    // Returns true when the user transitions to online (their first connection in this group).
    private static bool AddPresence(Dictionary<string, HashSet<string>> present, string userId, string clientId)
    {
        if (present.TryGetValue(userId, out var clients))
        {
            clients.Add(clientId);
            return false;
        }

        present[userId] = [clientId];
        return true;
    }

    // Returns true when the user transitions to offline (their last connection in this group left).
    private static bool RemovePresence(Dictionary<string, HashSet<string>> present, string userId, string clientId)
    {
        if (!present.TryGetValue(userId, out var clients)) return false;

        clients.Remove(clientId);

        if (clients.Count > 0) return false;

        present.Remove(userId);
        return true;
    }

    private Task BroadcastPresenceAsync(string group, string userId, string clientId, PresenceKind kind)
    {
        var message = new PresenceMessage(group, userId, clientId, kind);
        var json = JsonSerializer.Serialize(message, BoardEventSerializerContext.Default.PresenceMessage);

        return connection.GetSubscriber().PublishAsync(PresenceChannel, json);
    }

    public Task BroadcastAsync(string group, string clientId)
    {
        var message = new ClientChannel(clientId, group, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(message, BoardEventSerializerContext.Default.ClientChannel);

        return connection.GetSubscriber().PublishAsync(RealTimeGroupChannel, json);
    }
}
