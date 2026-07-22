namespace Netptune.Automation.Configuration;

public sealed class ScheduleOptions
{
    public const string SectionName = "Automation:Schedule";

    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan DelayedActionRunInterval { get; set; } = TimeSpan.FromMinutes(1);

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
    }
}
