namespace Netptune.Automation.Configuration;

public sealed class LimitsOptions
{
    public const string SectionName = "Automation:Limits";

    public bool CircuitBreakerEnabled { get; set; } = true;

    public TimeSpan Window { get; set; } = TimeSpan.FromHours(1);

    public int FailureThreshold { get; set; } = 20;

    public int RunThreshold { get; set; } = 500;

    public int WorkspaceRunQuota { get; set; } = 5000;

    internal void Validate()
    {
        if (Window <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Window)} must be greater than zero.");
        }

        if (FailureThreshold < 1)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(FailureThreshold)} must be greater than zero.");
        }

        if (RunThreshold < 1)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(RunThreshold)} must be greater than zero.");
        }

        if (WorkspaceRunQuota < 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(WorkspaceRunQuota)} cannot be negative.");
        }
    }
}
