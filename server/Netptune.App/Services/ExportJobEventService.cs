using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Cache.Redis;
using Netptune.Transfer.Services;

using StackExchange.Redis;

namespace Netptune.App.Services;

[JsonSerializable(typeof(ExportJobProgressEvent))]
internal partial class ExportJobEventSerializerContext : JsonSerializerContext;

public interface IExportJobEventService
{
    Task SubscribeAsync(string workspaceSlug, HttpResponse response, CancellationToken cancellationToken);
}

public sealed class ExportJobEventService(
    ILogger<ExportJobEventService> logger,
    IConnectionMultiplexer connection) : IExportJobEventService
{
    public async Task SubscribeAsync(string workspaceSlug, HttpResponse response, CancellationToken cancellationToken)
    {
        response.ContentType = "text/event-stream";
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var queue = await connection.GetSubscriber().SubscribeAsync(ExportJobChannels.ForWorkspace(workspaceSlug));

        try
        {
            await foreach (var message in queue)
            {
                await WriteFrame(message, response, cancellationToken);
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Export job SSE client disconnected");
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }

    // The publisher already wrote the frame's JSON, so it is forwarded as-is. Deserialising it only to
    // serialise the identical object back would cost a round trip per event and change nothing. It is
    // still parsed once to reject anything malformed before it reaches the browser.
    private async Task WriteFrame(ChannelMessage message, HttpResponse response, CancellationToken cancellationToken)
    {
        var json = message.Message.ToString();

        try
        {
            var progressEvent = JsonSerializer.Deserialize(
                json,
                ExportJobEventSerializerContext.Default.ExportJobProgressEvent);

            if (progressEvent is null)
            {
                return;
            }

            await response.WriteAsync($"event: export-job-progress\ndata: {json}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Export job event could not be written");
        }
    }
}
