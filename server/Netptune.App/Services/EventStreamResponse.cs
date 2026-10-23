namespace Netptune.App.Services;

// Cloudflare drops a proxied response that sends nothing for 100 seconds, and a half-open connection is
// only noticed once a write fails, so an idle stream still writes a comment frame on an interval.
public sealed class EventStreamResponse : IAsyncDisposable
{
    public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

    private const string HeartbeatFrame = ": heartbeat\n\n";

    private readonly HttpResponse Response;
    private readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly CancellationTokenSource HeartbeatCancellation;
    private readonly Task Heartbeat;

    private EventStreamResponse(HttpResponse response, TimeSpan heartbeatInterval, CancellationToken cancellationToken)
    {
        Response = response;
        HeartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Heartbeat = RunHeartbeatAsync(heartbeatInterval, HeartbeatCancellation.Token);
    }

    public static Task<EventStreamResponse> OpenAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        return OpenAsync(response, DefaultHeartbeatInterval, cancellationToken);
    }

    public static async Task<EventStreamResponse> OpenAsync(
        HttpResponse response,
        TimeSpan heartbeatInterval,
        CancellationToken cancellationToken)
    {
        response.ContentType = "text/event-stream";
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        return new EventStreamResponse(response, heartbeatInterval, cancellationToken);
    }

    public async Task WriteAsync(string frame, CancellationToken cancellationToken)
    {
        await WriteLock.WaitAsync(cancellationToken);

        try
        {
            await Response.WriteAsync(frame, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await HeartbeatCancellation.CancelAsync();
        await Heartbeat;

        HeartbeatCancellation.Dispose();
        WriteLock.Dispose();
    }

    private async Task RunHeartbeatAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await WriteAsync(HeartbeatFrame, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            // The subscription loop is waiting on its next message, not on this write, so the failure has to
            // reach it through RequestAborted.
            Response.HttpContext.Abort();
        }
    }
}
