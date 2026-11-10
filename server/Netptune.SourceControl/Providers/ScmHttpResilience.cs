using System.Net;

using Microsoft.Extensions.Http.Resilience;

using Polly;
using Polly.Timeout;

namespace Netptune.SourceControl.Providers;

// The service defaults retry 429s, honour Retry-After past their own total timeout, and share one
// circuit breaker across every connection. Provider limits are per installation or per user and are
// reported to the caller instead, so only idempotent requests are retried, and only for transient failures.
internal static class ScmHttpResilience
{
    private const int MaxRetryAttempts = 2;

    private static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> pipeline)
    {
        pipeline.AddTimeout(TotalTimeout);

        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            Delay = RetryDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldRetryAfterHeader = false,
            ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Context, args.Outcome)),
        });

        pipeline.AddTimeout(AttemptTimeout);
    }

    private static bool ShouldRetry(ResilienceContext context, Outcome<HttpResponseMessage> outcome)
    {
        var method = context.GetRequestMessage()?.Method;
        var isIdempotent = method == HttpMethod.Get || method == HttpMethod.Head;

        if (!isIdempotent)
        {
            return false;
        }

        if (outcome.Exception is not null)
        {
            return outcome.Exception is HttpRequestException or TimeoutRejectedException;
        }

        var status = outcome.Result?.StatusCode;
        var isTransientStatus = status is HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.InternalServerError;

        return isTransientStatus;
    }
}
