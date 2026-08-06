using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Transfer.Services;

using StackExchange.Redis;

namespace Netptune.Cache.Redis;

[JsonSerializable(typeof(ExportJobProgressEvent))]
internal partial class ExportJobProgressSerializerContext : JsonSerializerContext;

public sealed class RedisExportJobNotifier(IConnectionMultiplexer connection) : IExportJobNotifier
{
    public Task PublishAsync(string workspaceSlug, ExportJobProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = JsonSerializer.Serialize(
            progressEvent,
            ExportJobProgressSerializerContext.Default.ExportJobProgressEvent);

        return connection.GetSubscriber().PublishAsync(ExportJobChannels.ForWorkspace(workspaceSlug), json);
    }
}

public static class ExportJobChannels
{
    public static RedisChannel ForWorkspace(string workspaceSlug)
    {
        return RedisChannel.Literal($"export-jobs:{workspaceSlug}");
    }
}
