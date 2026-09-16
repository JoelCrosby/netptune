using System.Collections.Concurrent;

namespace Netptune.App.Services;

// An event stream is held open for as long as the client wants it, so the request rate limit never
// gets a chance to bound how many exist. This counts the live ones instead.
public sealed class SseConnectionLimiter
{
    public const int MaxConnectionsPerUser = 8;

    private readonly ConcurrentDictionary<string, int> ConnectionsByUser = new();

    // AddOrUpdate can run its delegate more than once under contention and only keep one result, so a
    // flag captured inside it does not reliably say whether this caller is the one that was counted.
    public SseConnectionLease? TryAcquire(string userId)
    {
        while (true)
        {
            if (!ConnectionsByUser.TryGetValue(userId, out var current))
            {
                if (ConnectionsByUser.TryAdd(userId, 1))
                {
                    return new SseConnectionLease(this, userId);
                }

                continue;
            }

            if (current >= MaxConnectionsPerUser)
            {
                return null;
            }

            if (ConnectionsByUser.TryUpdate(userId, current + 1, current))
            {
                return new SseConnectionLease(this, userId);
            }
        }
    }

    internal void Release(string userId)
    {
        while (ConnectionsByUser.TryGetValue(userId, out var current))
        {
            if (current <= 1)
            {
                if (ConnectionsByUser.TryRemove(new KeyValuePair<string, int>(userId, current)))
                {
                    return;
                }

                continue;
            }

            if (ConnectionsByUser.TryUpdate(userId, current - 1, current))
            {
                return;
            }
        }
    }
}

public sealed class SseConnectionLease : IDisposable
{
    private readonly SseConnectionLimiter Limiter;
    private readonly string UserId;
    private bool Released;

    internal SseConnectionLease(SseConnectionLimiter limiter, string userId)
    {
        Limiter = limiter;
        UserId = userId;
    }

    public void Dispose()
    {
        if (Released)
        {
            return;
        }

        Released = true;

        Limiter.Release(UserId);
    }
}
