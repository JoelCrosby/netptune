using Netptune.Core.Models.Reporting;

namespace Netptune.PublicApi.Requests;

public sealed record PublicFlowReportFilter
{
    public int? ProjectId { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public ReportingUnit? Unit { get; init; }

    public string? TimeZone { get; init; }

    public ReportingGrouping? Grouping { get; init; }

    internal ReportingFilter ToFilter()
    {
        return new ReportingFilter
        {
            ProjectId = ProjectId,
            From = From,
            To = To,
            Unit = Unit ?? ReportingUnit.Tasks,
            TimeZone = TimeZone ?? DefaultTimeZone.Value,
            Grouping = Grouping ?? ReportingGrouping.Day,
        };
    }
}

public sealed record PublicWorkloadReportFilter
{
    public int? ProjectId { get; init; }

    public ReportingUnit? Unit { get; init; }

    internal ReportingFilter ToFilter()
    {
        return new ReportingFilter
        {
            ProjectId = ProjectId,
            Unit = Unit ?? ReportingUnit.Tasks,
        };
    }
}

public sealed record PublicSprintBurndownFilter
{
    public ReportingUnit? Unit { get; init; }

    public string? TimeZone { get; init; }

    internal SprintBurndownFilter ToFilter(int sprintId)
    {
        return new SprintBurndownFilter
        {
            SprintId = sprintId,
            Unit = Unit ?? ReportingUnit.Tasks,
            TimeZone = TimeZone ?? DefaultTimeZone.Value,
        };
    }
}

public sealed record PublicVelocityReportFilter
{
    public int ProjectId { get; init; }

    public ReportingUnit? Unit { get; init; }

    public int? Take { get; init; }

    internal VelocityFilter ToFilter()
    {
        return new VelocityFilter
        {
            ProjectId = ProjectId,
            Unit = Unit ?? ReportingUnit.Tasks,
            Take = Take ?? DefaultVelocityTake,
        };
    }

    private const int DefaultVelocityTake = 12;
}

internal static class DefaultTimeZone
{
    public const string Value = "UTC";
}
