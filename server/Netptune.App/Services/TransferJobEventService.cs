using System.Text.Json;

using Netptune.Cache.Redis;
using Netptune.Transfer.Services;

using StackExchange.Redis;

namespace Netptune.App.Services;

public interface ITransferJobEventService
{
    Task SubscribeAsync(string workspaceSlug, HttpResponse response, CancellationToken cancellationToken);
}

public sealed class TransferJobEventService(
    ILogger<TransferJobEventService> logger,
    IConnectionMultiplexer connection) : ITransferJobEventService
{
    public async Task SubscribeAsync(string workspaceSlug, HttpResponse response, CancellationToken cancellationToken)
    {
        response.ContentType = "text/event-stream";
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var queue = await connection.GetSubscriber().SubscribeAsync(TransferJobChannels.ForWorkspace(workspaceSlug));

        try
        {
            await foreach (var message in queue)
            {
                await WriteFrame(message, response, cancellationToken);
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Transfer job SSE client disconnected");
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }

    // The publisher already wrote the payload's JSON, so it is forwarded as-is. Deserialising it only to
    // serialise the identical object back would cost a round trip per event and change nothing.
    private async Task WriteFrame(ChannelMessage message, HttpResponse response, CancellationToken cancellationToken)
    {
        try
        {
            using var envelope = JsonDocument.Parse(message.Message.ToString());
            var root = envelope.RootElement;

            var hasName = root.TryGetProperty("event", out var name);
            var hasData = root.TryGetProperty("data", out var data);

            if (!hasName || !hasData)
            {
                return;
            }

            var eventName = name.GetString();

            if (!TransferJobEventNames.IsKnown(eventName))
            {
                return;
            }

            await response.WriteAsync($"event: {eventName}\ndata: {data.GetRawText()}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Transfer job event could not be written");
        }
    }
}
