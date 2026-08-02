using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetWorkloadReportTool : IAiTool
{
    private readonly IMediator Mediator;

    public GetWorkloadReportTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_workload_report";

    public string Description =>
        "How open work is spread across the team: a row per assignee with their task count and estimate total, "
        + "plus how many tasks are unassigned, assigned to several people, or missing an estimate. "
        + "Use this for questions about who is overloaded or where work is piling up.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Members.Read,
    };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "projectId": { "type": "integer", "description": "Restrict the report to a single project." },
          "unit": {
            "type": "string",
            "enum": ["tasks", "storyPoints", "hours"],
            "description": "What to measure. Defaults to tasks."
          }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
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
