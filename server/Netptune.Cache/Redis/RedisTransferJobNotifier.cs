using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Netptune.Transfer.Services;

using StackExchange.Redis;

namespace Netptune.Cache.Redis;

[JsonSerializable(typeof(ExportJobProgressEvent))]
[JsonSerializable(typeof(ImportSessionProgressEvent))]
internal partial class TransferJobProgressSerializerContext : JsonSerializerContext;

public sealed class RedisTransferJobNotifier(IConnectionMultiplexer connection) : ITransferJobNotifier
{
    public Task PublishExportAsync(string workspaceSlug, ExportJobProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(progressEvent, TransferJobProgressSerializerContext.Default.ExportJobProgressEvent);

        return Publish(workspaceSlug, TransferJobEventNames.ExportProgress, json, cancellationToken);
    }

    public Task PublishImportAsync(string workspaceSlug, ImportSessionProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(progressEvent, TransferJobProgressSerializerContext.Default.ImportSessionProgressEvent);

        return Publish(workspaceSlug, TransferJobEventNames.ImportProgress, json, cancellationToken);
    }

    private Task Publish(string workspaceSlug, string eventName, string json, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = new JsonObject
        {
            ["event"] = eventName,
            ["data"] = JsonNode.Parse(json),
        };

        return connection
            .GetSubscriber()
            .PublishAsync(TransferJobChannels.ForWorkspace(workspaceSlug), envelope.ToJsonString());
    }
}

public static class TransferJobChannels
{
    public static RedisChannel ForWorkspace(string workspaceSlug)
    {
        return RedisChannel.Literal($"transfer-jobs:{workspaceSlug}");
    }
}
