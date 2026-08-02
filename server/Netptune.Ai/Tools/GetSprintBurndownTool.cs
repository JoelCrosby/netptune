using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetSprintBurndownTool : IAiTool
{
    private readonly IMediator Mediator;

    public GetSprintBurndownTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_sprint_burndown";

    public string Description =>
        "How a single sprint tracked against its plan: remaining work per day against the ideal line, what was "
        + "committed at the start, what was added or removed mid-sprint, and how much finished. "
        + "Use this for questions about how a sprint went or whether it is on track.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Sprints.Read,
    };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The sprint id, from list_sprints or get_current_sprint." },
          "unit": {
            "type": "string",
            "enum": ["tasks", "storyPoints", "hours"],
            "description": "What to measure. Defaults to tasks."
          },
          "timeZone": { "type": "string", "description": "IANA time zone the days are bucketed in. Defaults to UTC." }
        }
        """,
        "sprintId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!sprintId.HasValue)
        {
            return AiToolExecution.Failed("A sprintId is required.");
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
}
