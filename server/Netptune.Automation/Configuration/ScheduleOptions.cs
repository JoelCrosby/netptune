namespace Netptune.Automation.Configuration;

public sealed class ScheduleOptions
{
    public const string SectionName = "Automation:Schedule";

    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan DelayedActionRunInterval { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan DelayedActionClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan DelayedActionRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    public int DelayedActionMaxAttempts { get; set; } = 3;

    public int DelayedActionBatchSize { get; set; } = 100;

    internal void Validate()
    {
        if (StartupDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(StartupDelay)} cannot be negative.");
        }

        if (RunInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(RunInterval)} must be greater than zero.");
        }

        if (DelayedActionRunInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DelayedActionRunInterval)} must be greater than zero.");
        }

        if (DelayedActionClaimLease <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DelayedActionClaimLease)} must be greater than zero.");
        }

        if (DelayedActionRetryDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DelayedActionRetryDelay)} must be greater than zero.");
        }

        if (DelayedActionMaxAttempts is < 1 or > 20)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DelayedActionMaxAttempts)} must be between 1 and 20.");
        }

        if (DelayedActionBatchSize is < 1 or > 1000)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DelayedActionBatchSize)} must be between 1 and 1000.");
        }
    }
}
