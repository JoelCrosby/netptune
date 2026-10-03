using Netptune.Entities.Contexts;

using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Netptune.Entities;

public static class DatabaseSchema
{
    public static Task EnsureCreatedAsync(DataContext context, CancellationToken cancellationToken)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(2), retryCount: 5);

        return Policy
            .Handle<Exception>(exception => exception is not OperationCanceledException)
            .WaitAndRetryAsync(delay)
            .ExecuteAsync(token => context.Database.EnsureCreatedAsync(token), cancellationToken);
    }
}
