using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

using StackExchange.Redis;

namespace Netptune.App.Services;

public sealed record WorkspaceEvent
{
    public required string Workspace { get; init; }

    public required string SourceClientId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record RealtimeSubscription
{
    public required string Workspace { get; init; }

    public required string Group { get; init; }

    public required string SourceClientId { get; init; }

    public required string ConnectionId { get; init; }

    public required string UserId { get; init; }
}

public enum PresenceKind
{
    Join,
    Hello,
    Leave,
}

public sealed record PresenceMessage
{
    public required string Workspace { get; init; }

    public required string Group { get; init; }

    public required string UserId { get; init; }

    public required string ClientId { get; init; }

    public PresenceKind Kind { get; init; }
}

public sealed class BoardEventService : IBoardEventService, IHostedService
{
    private const int ConnectionQueueCapacity = 32;
    private static readonly RedisChannel WorkspaceEventChannel = RedisChannel.Literal("workspace-events");
    private static readonly RedisChannel PresenceChannel = RedisChannel.Literal("board-presence");

    private readonly ILogger<BoardEventService> Logger;
    private readonly ISubscriber Subscriber;
    private readonly ConcurrentDictionary<string, LocalConnection> Connections = new();
    private readonly Dictionary<PresenceGroup, Dictionary<string, HashSet<string>>> Presence = [];
    private readonly object PresenceLock = new();

    private readonly record struct PresenceGroup(string Workspace, string Group);

    private sealed record LocalConnection
    {
        public required RealtimeSubscription Subscription { get; init; }

        public required Channel<string> Outbound { get; init; }
    }

    public BoardEventService(ILogger<BoardEventService> logger, IConnectionMultiplexer connection)
    {
        Logger = logger;
        Subscriber = connection.GetSubscriber();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var eventQueue = await Subscriber.SubscribeAsync(WorkspaceEventChannel);
        var presenceQueue = await Subscriber.SubscribeAsync(PresenceChannel);
        eventQueue.OnMessage(HandleWorkspaceEvent);
        presenceQueue.OnMessage(HandlePresenceMessageAsync);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Subscriber.UnsubscribeAsync(WorkspaceEventChannel);
        await Subscriber.UnsubscribeAsync(PresenceChannel);

        foreach (var connection in Connections.Values)
        {
            connection.Outbound.Writer.TryComplete();
        }
    }

    public async Task SubscribeAsync(
        RealtimeSubscription subscription,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var outbound = Channel.CreateBounded<string>(new BoundedChannelOptions(ConnectionQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
        var localConnection = new LocalConnection
        {
            Subscription = subscription,
            Outbound = outbound,
        };

        Connections[subscription.ConnectionId] = localConnection;
        AddPresence(subscription);
        WritePresenceFrames(subscription.Workspace, subscription.Group);
        await BroadcastPresenceAsync(subscription, PresenceKind.Join);

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
            Logger.LogDebug("Realtime client disconnected");
        }
        finally
        {
            Connections.TryRemove(subscription.ConnectionId, out _);
            RemovePresence(subscription);
            WritePresenceFrames(subscription.Workspace, subscription.Group);
            await BroadcastPresenceAsync(subscription, PresenceKind.Leave);
        }
    }

    public Task BroadcastAsync(string workspace, string sourceClientId)
    {
        var message = new WorkspaceEvent
        {
            Workspace = workspace,
            SourceClientId = sourceClientId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var json = JsonSerializer.Serialize(message, BoardEventSerializerContext.Default.WorkspaceEvent);

        return Subscriber.PublishAsync(WorkspaceEventChannel, json);
    }

    private void HandleWorkspaceEvent(ChannelMessage message)
    {
        try
        {
            var workspaceEvent = JsonSerializer.Deserialize(
                message.Message.ToString(),
                BoardEventSerializerContext.Default.WorkspaceEvent);

            if (workspaceEvent is null)
            {
                return;
            }

            foreach (var connection in Connections.Values)
            {
                var subscription = connection.Subscription;
                var isSameWorkspace = subscription.Workspace == workspaceEvent.Workspace;
                var isSourceClient = subscription.SourceClientId == workspaceEvent.SourceClientId;

                if (isSameWorkspace && !isSourceClient)
                {
                    TryWrite(connection, "data: update\n\n");
                }
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Workspace event deserialization failed");
        }
    }

    private async Task HandlePresenceMessageAsync(ChannelMessage message)
    {
        try
        {
            var presence = JsonSerializer.Deserialize(
                message.Message.ToString(),
                BoardEventSerializerContext.Default.PresenceMessage);

            if (presence is null)
            {
                return;
            }

            var changed = ApplyPresence(presence);

            if (presence.Kind is PresenceKind.Join)
            {
                await ReplyToPresenceJoin(presence);
            }

            if (changed)
            {
                WritePresenceFrames(presence.Workspace, presence.Group);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Presence message handling failed");
        }
    }

    private Task ReplyToPresenceJoin(PresenceMessage presence)
    {
        var replies = Connections.Values
            .Select(connection => connection.Subscription)
            .Where(subscription =>
                subscription.Workspace == presence.Workspace &&
                subscription.Group == presence.Group &&
                subscription.ConnectionId != presence.ClientId)
            .Select(subscription => BroadcastPresenceAsync(subscription, PresenceKind.Hello));

        return Task.WhenAll(replies);
    }

    private Task BroadcastPresenceAsync(RealtimeSubscription subscription, PresenceKind kind)
    {
        var message = new PresenceMessage
        {
            Workspace = subscription.Workspace,
            Group = subscription.Group,
            UserId = subscription.UserId,
            ClientId = subscription.ConnectionId,
            Kind = kind,
        };
        var json = JsonSerializer.Serialize(message, BoardEventSerializerContext.Default.PresenceMessage);

        return Subscriber.PublishAsync(PresenceChannel, json);
    }

    private void AddPresence(RealtimeSubscription subscription)
    {
        var group = new PresenceGroup(subscription.Workspace, subscription.Group);

        lock (PresenceLock)
        {
            if (!Presence.TryGetValue(group, out var users))
            {
                users = [];
                Presence[group] = users;
            }

            if (!users.TryGetValue(subscription.UserId, out var clients))
            {
                clients = [];
                users[subscription.UserId] = clients;
            }

            clients.Add(subscription.ConnectionId);
        }
    }

    private void RemovePresence(RealtimeSubscription subscription)
    {
        var message = new PresenceMessage
        {
            Workspace = subscription.Workspace,
            Group = subscription.Group,
            UserId = subscription.UserId,
            ClientId = subscription.ConnectionId,
            Kind = PresenceKind.Leave,
        };
        ApplyPresence(message);
    }

    private bool ApplyPresence(PresenceMessage message)
    {
        var group = new PresenceGroup(message.Workspace, message.Group);

        lock (PresenceLock)
        {
            return message.Kind switch
            {
                PresenceKind.Join => AddPresence(Presence, group, message.UserId, message.ClientId),
                PresenceKind.Hello => AddPresence(Presence, group, message.UserId, message.ClientId),
                PresenceKind.Leave => RemovePresence(Presence, group, message.UserId, message.ClientId),
                _ => false,
            };
        }
    }

    private static bool AddPresence(
        Dictionary<PresenceGroup, Dictionary<string, HashSet<string>>> presence,
        PresenceGroup group,
        string userId,
        string clientId)
    {
        if (!presence.TryGetValue(group, out var users))
        {
            users = [];
            presence[group] = users;
        }

        if (!users.TryGetValue(userId, out var clients))
        {
            clients = [];
            users[userId] = clients;
        }

        return clients.Add(clientId);
    }

    private static bool RemovePresence(
        Dictionary<PresenceGroup, Dictionary<string, HashSet<string>>> presence,
        PresenceGroup group,
        string userId,
        string clientId)
    {
        if (!presence.TryGetValue(group, out var users) || !users.TryGetValue(userId, out var clients))
        {
            return false;
        }

        var changed = clients.Remove(clientId);

        if (clients.Count == 0)
        {
            users.Remove(userId);
        }

        if (users.Count == 0)
        {
            presence.Remove(group);
        }

        return changed;
    }

    private void WritePresenceFrames(string workspace, string group)
    {
        var users = GetPresentUsers(workspace, group);
        var json = JsonSerializer.Serialize(users, BoardEventSerializerContext.Default.StringArray);
        var frame = $"event: presence\ndata: {json}\n\n";

        foreach (var connection in Connections.Values)
        {
            var subscription = connection.Subscription;

            if (subscription.Workspace == workspace && subscription.Group == group)
            {
                TryWrite(connection, frame);
            }
        }
    }

    private string[] GetPresentUsers(string workspace, string group)
    {
        var presenceGroup = new PresenceGroup(workspace, group);

        lock (PresenceLock)
        {
            return Presence.TryGetValue(presenceGroup, out var users) ? users.Keys.ToArray() : [];
        }
    }

    private static void TryWrite(LocalConnection connection, string frame)
    {
        if (!connection.Outbound.Writer.TryWrite(frame))
        {
            connection.Outbound.Writer.TryComplete();
        }
    }
}
