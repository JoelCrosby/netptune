namespace Netptune.Core.SourceControl;

public sealed class ScmRateLimitedException : Exception
{
    public SourceControlProvider Provider { get; }

    public DateTimeOffset RetryAt { get; }

    public ScmRateLimitedException(SourceControlProvider provider, DateTimeOffset retryAt)
        : base($"{provider} is rate limiting requests until {retryAt:O}.")
    {
        Provider = provider;
        RetryAt = retryAt;
    }
}
