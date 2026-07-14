using Netptune.Core.Enums;

namespace Netptune.Core.Models.Activity;

public sealed class ActivityMergeOptions
{
    public const string SectionName = "Activity:Merge";

    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan MaxWindowDuration { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);

    public List<ActivityType> MergeableTypes { get; set; } =
    [
        ActivityType.Modify,
        ActivityType.ModifyName,
        ActivityType.ModifyDescription,
        ActivityType.ModifyStatus,
        ActivityType.ModifyPriority,
        ActivityType.ModifyEstimate,
        ActivityType.ModifyDueDate,
    ];

    public bool IsMergeable(ActivityType type) => MergeableTypes.Contains(type);

    public void Validate()
    {
        if (WindowDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(WindowDuration)} must be greater than zero.");
        }

        if (MaxWindowDuration < WindowDuration)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(MaxWindowDuration)} cannot be less than {nameof(WindowDuration)}.");
        }

        if (SweepInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(SweepInterval)} must be greater than zero.");
        }
    }
}
