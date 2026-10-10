using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetReportTool : IAiTool
{
    public const string Flow = "flow";
    public const string Burndown = "burndown";
    public const string Velocity = "velocity";
    public const string Workload = "workload";

    private const int DefaultVelocityTake = 12;

    private static readonly string[] KnownReports = [Flow, Burndown, Velocity, Workload];

    private readonly IMediator Mediator;

    public GetReportTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_report";

    public string Description =>
        "Read a delivery report. "
        + "flow is pace over a date range: tasks completed, cycle time from start to done (median and 85th percentile) "
        + "and how many are still open; for throughput or whether delivery is speeding up. "
        + "burndown is how one sprint tracked against its plan: remaining work per day against the ideal line, "
        + "and what was committed, added, removed and finished; for how a sprint went or whether it is on track. "
        + "velocity is what a project's completed sprints delivered, newest first, committed against completed; "
        + "to compare sprints or estimate what the next one can hold. "
        + "workload is how open work is spread across assignees, plus unassigned, multi-assigned and unestimated tasks; "
        + "for who is overloaded or where work is piling up.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            NetptunePermissions.Reports.Read,
            NetptunePermissions.Tasks.Read,
        };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "kind": {
            "type": "string",
            "enum": ["flow", "burndown", "velocity", "workload"],
            "description": "Which report to read."
          },
          "projectId": { "type": "integer", "description": "Restrict to one project. Required for velocity." },
          "sprintId": { "type": "integer", "description": "Required for burndown: the sprint, from list_records or get_current_sprint." },
          "unit": {
            "type": "string",
            "enum": ["tasks", "storyPoints", "hours"],
            "description": "What to measure. Defaults to tasks."
          },
          "from": { "type": "string", "description": "Flow only: start of the window as an ISO date, for example 2026-01-01." },
          "to": { "type": "string", "description": "Flow only: end of the window as an ISO date." },
          "grouping": {
            "type": "string",
            "enum": ["day", "week"],
            "description": "Flow only: bucket size for the completed-over-time series. Defaults to day."
          },
          "timeZone": { "type": "string", "description": "Burndown only: IANA time zone the days are bucketed in. Defaults to UTC." },
          "take": { "type": "integer", "description": "Velocity only: how many recent sprints to include. Defaults to 12." }
        }
        """,
        "kind");

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var report = AiToolSchema.GetString(arguments, "kind");
        var required = new HashSet<string>(RequiredPermissions, StringComparer.Ordinal);
        var readsSprints = report is Burndown or Velocity;

        if (readsSprints)
        {
            required.Add(NetptunePermissions.Sprints.Read);
        }

        var readsMembers = report == Workload;

        if (readsMembers)
        {
            required.Add(NetptunePermissions.Members.Read);
        }

        return required;
    }

    public string DescribeCall(JsonElement arguments)
    {
        return AiToolSchema.DescribeCall(Name, arguments, "kind", KnownReports);
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var report = AiToolSchema.GetString(arguments, "kind");

        return report switch
        {
            Flow => await ReadFlow(arguments, cancellationToken),
            Burndown => await ReadBurndown(arguments, cancellationToken),
            Velocity => await ReadVelocity(arguments, cancellationToken),
            Workload => await ReadWorkload(arguments, cancellationToken),
            _ => AiToolExecution.Failed($"kind must be one of: {string.Join(", ", KnownReports)}."),
        };
    }

    private async Task<AiToolExecution> ReadFlow(JsonElement arguments, CancellationToken cancellationToken)
    {
        var filter = new ReportingFilter
        {
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            From = AiToolSchema.GetDate(arguments, "from"),
            To = AiToolSchema.GetDate(arguments, "to"),
            Unit = AiToolSchema.GetEnum<ReportingUnit>(arguments, "unit") ?? ReportingUnit.Tasks,
            Grouping = AiToolSchema.GetEnum<ReportingGrouping>(arguments, "grouping") ?? ReportingGrouping.Day,
        };

        var result = await Mediator.Send(new GetFlowReportQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "The flow report could not be read.");
        }

        var report = result.Payload;
        var summary = new
        {
            throughput = report.Throughput,
            medianCycleTimeHours = report.MedianCycleTimeHours,
            p85CycleTimeHours = report.P85CycleTimeHours,
            cycleTimeSampleSize = report.CycleTimeSampleSize,
            currentOpenTaskCount = report.CurrentOpenTaskCount,
            coverageStart = report.Coverage.CoverageStart,
            isPartial = report.Coverage.IsPartial,
            completedPerBucket = report.Buckets.Select(bucket => new
            {
                date = bucket.Date,
                completed = bucket.Completed,
            }),
            cycleTimeByWeek = report.CycleTimeBuckets.Select(bucket => new
            {
                weekStarting = bucket.WeekStarting,
                medianHours = bucket.MedianCycleTimeHours,
                p85Hours = bucket.P85CycleTimeHours,
                sampleSize = bucket.SampleSize,
            }),
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(summary));
    }

    private async Task<AiToolExecution> ReadBurndown(JsonElement arguments, CancellationToken cancellationToken)
    {
        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!sprintId.HasValue)
        {
            return AiToolExecution.Failed("A sprintId is required for the burndown report.");
        }

        var filter = new SprintBurndownFilter
        {
            SprintId = sprintId.Value,
            Unit = AiToolSchema.GetEnum<ReportingUnit>(arguments, "unit") ?? ReportingUnit.Tasks,
            TimeZone = AiToolSchema.GetString(arguments, "timeZone") ?? "UTC",
        };

        var result = await Mediator.Send(new GetSprintBurndownReportQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(
                result.Message ?? $"Sprint {sprintId} was not found in this workspace.");
        }

        var report = result.Payload;
        var summary = new
        {
            sprintId = report.SprintId,
            sprintName = report.SprintName,
            unit = report.Unit.ToString(),
            committedCount = report.CommittedCount,
            addedCount = report.AddedCount,
            removedCount = report.RemovedCount,
            completedCount = report.CompletedCount,
            completionPercentage = report.CompletionPercentage,
            missingEstimateCount = report.MissingEstimateCount,
            coverageStart = report.Coverage.CoverageStart,
            isPartial = report.Coverage.IsPartial,
            points = report.Points.Select(point => new
            {
                date = point.Date,
                remaining = point.Remaining,
                totalScope = point.TotalScope,
                ideal = point.Ideal,
            }),
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(summary));
    }

    private async Task<AiToolExecution> ReadVelocity(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.GetInt(arguments, "projectId");

        if (!projectId.HasValue)
        {
            return AiToolExecution.Failed("A projectId is required for the velocity report.");
        }

        var filter = new VelocityFilter
        {
            ProjectId = projectId.Value,
            Unit = AiToolSchema.GetEnum<ReportingUnit>(arguments, "unit") ?? ReportingUnit.Tasks,
            Take = AiToolSchema.GetInt(arguments, "take") ?? DefaultVelocityTake,
        };

        var result = await Mediator.Send(new GetVelocityReportQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(
                result.Message ?? $"Project {projectId} was not found in this workspace.");
        }

        var report = result.Payload;
        var summary = new
        {
            unit = report.Unit.ToString(),
            excludedSprintCount = report.ExcludedSprintCount,
            coverageStart = report.Coverage.CoverageStart,
            isPartial = report.Coverage.IsPartial,
            sprints = report.Sprints.Select(sprint => new
            {
                sprintId = sprint.SprintId,
                name = sprint.SprintName,
                completedAt = sprint.CompletedAt,
                committed = sprint.Committed,
                completed = sprint.Completed,
                completedCount = sprint.CompletedCount,
                completionPercentage = sprint.CompletionPercentage,
                missingEstimateCount = sprint.MissingEstimateCount,
            }),
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(summary));
    }

    private async Task<AiToolExecution> ReadWorkload(JsonElement arguments, CancellationToken cancellationToken)
    {
        var filter = new ReportingFilter
        {
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            Unit = AiToolSchema.GetEnum<ReportingUnit>(arguments, "unit") ?? ReportingUnit.Tasks,
        };

        var result = await Mediator.Send(new GetWorkloadReportQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "The workload report could not be read.");
        }

        var report = result.Payload;
        var summary = new
        {
            unit = report.Unit.ToString(),
            uniqueTaskCount = report.UniqueTaskCount,
            unassignedTaskCount = report.UnassignedTaskCount,
            multiAssignedTaskCount = report.MultiAssignedTaskCount,
            missingEstimateCount = report.MissingEstimateCount,
            rows = report.Rows.Select(row => new
            {
                userId = row.UserId,
                name = row.DisplayName,
                taskCount = row.TaskCount,
                value = row.Value,
            }),
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(summary));
    }
}
