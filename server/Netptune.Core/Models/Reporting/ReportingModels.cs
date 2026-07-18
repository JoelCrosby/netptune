namespace Netptune.Core.Models.Reporting;

public enum ReportingUnit
{
    Tasks,
    StoryPoints,
    Hours,
}

public sealed record ReportingFilter
{
    public int? ProjectId { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public ReportingUnit Unit { get; init; } = ReportingUnit.Tasks;

    public string TimeZone { get; init; } = "UTC";
}

public sealed record ReportingCoverage(DateTime? CoverageStart, bool IsPartial);

public sealed record FlowReport
{
    public IReadOnlyCollection<FlowBucket> Buckets { get; init; } = [];

    public int Throughput { get; init; }

    public decimal? MedianCycleTimeHours { get; init; }

    public decimal? P85CycleTimeHours { get; init; }

    public int CycleTimeSampleSize { get; init; }

    public required ReportingCoverage Coverage { get; init; }
}

public sealed record FlowBucket(DateOnly Date, int Completed, decimal? MedianCycleTimeHours);

public sealed record WorkloadReport
{
    public IReadOnlyCollection<WorkloadRow> Rows { get; init; } = [];

    public int UniqueTaskCount { get; init; }

    public int UnassignedTaskCount { get; init; }

    public int MultiAssignedTaskCount { get; init; }

    public int MissingEstimateCount { get; init; }

    public ReportingUnit Unit { get; init; }
}

public sealed record WorkloadRow
{
    public string? UserId { get; init; }

    public required string DisplayName { get; init; }

    public int TaskCount { get; init; }

    public decimal Value { get; init; }
}

public sealed record SprintBurndownReport
{
    public int SprintId { get; init; }

    public required string SprintName { get; init; }

    public ReportingUnit Unit { get; init; }

    public IReadOnlyCollection<BurndownPoint> Points { get; init; } = [];

    public int CommittedCount { get; init; }

    public int AddedCount { get; init; }

    public int RemovedCount { get; init; }

    public int MissingEstimateCount { get; init; }

    public required ReportingCoverage Coverage { get; init; }
}

public sealed record BurndownPoint
{
    public DateOnly Date { get; init; }

    public decimal Remaining { get; init; }

    public decimal TotalScope { get; init; }

    public decimal Ideal { get; init; }
}

public sealed record VelocityReport
{
    public IReadOnlyCollection<VelocityPoint> Sprints { get; init; } = [];

    public ReportingUnit Unit { get; init; }

    public int ExcludedSprintCount { get; init; }

    public required ReportingCoverage Coverage { get; init; }
}

public sealed record VelocityPoint
{
    public int SprintId { get; init; }

    public required string SprintName { get; init; }

    public DateTime CompletedAt { get; init; }

    public decimal Committed { get; init; }

    public decimal Completed { get; init; }
}

public sealed class InvalidReportingFilterException(string message) : Exception(message);
