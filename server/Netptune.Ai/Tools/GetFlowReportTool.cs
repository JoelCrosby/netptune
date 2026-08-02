using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetFlowReportTool : IAiTool
{
    private readonly IMediator Mediator;

    public GetFlowReportTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_flow_report";

    public string Description =>
        "Delivery pace over a date range: how many tasks were completed, how long they took from start to done "
        + "(median and 85th percentile cycle time), and how many are still open. "
        + "Use this for questions about throughput, how fast work is moving, or whether delivery is speeding up.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "projectId": { "type": "integer", "description": "Restrict the report to a single project." },
          "from": { "type": "string", "description": "Start of the window as an ISO date, for example 2026-01-01." },
          "to": { "type": "string", "description": "End of the window as an ISO date." },
          "unit": {
            "type": "string",
            "enum": ["tasks", "storyPoints", "hours"],
            "description": "What to measure. Defaults to tasks."
          },
          "grouping": {
            "type": "string",
            "enum": ["day", "week"],
            "description": "Bucket size for the completed-over-time series. Defaults to day."
          }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
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
}
