using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Services.Realtime;

using StackExchange.Redis;

namespace Netptune.Cache.Redis;

[JsonSerializable(typeof(WorkspaceEvent))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class WorkspaceEventSerializerContext : JsonSerializerContext;

public sealed class RedisWorkspaceEventPublisher(IConnectionMultiplexer connection) : IWorkspaceEventPublisher
{
    private static readonly RedisChannel Channel = RedisChannel.Literal(IWorkspaceEventPublisher.ChannelName);

    public Task PublishAsync(string workspace, string sourceClientId, string[] scopes)
    {
        var message = new WorkspaceEvent
        {
            Workspace = workspace,
            SourceClientId = sourceClientId,
            Scopes = scopes,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var json = JsonSerializer.Serialize(message, WorkspaceEventSerializerContext.Default.WorkspaceEvent);

        return connection.GetSubscriber().PublishAsync(Channel, json);
    }
}
