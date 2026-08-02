using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetVelocityReportTool : IAiTool
{
    private const int DefaultTake = 12;

    private readonly IMediator Mediator;

    public GetVelocityReportTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_velocity_report";

    public string Description =>
        "What a project's completed sprints delivered, newest first: committed against completed for each one. "
        + "Use this to compare sprints, spot a trend, or estimate what the next sprint can hold.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Sprints.Read,
    };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "projectId": { "type": "integer", "description": "The project id, from list_projects." },
          "unit": {
            "type": "string",
            "enum": ["tasks", "storyPoints", "hours"],
            "description": "What to measure. Defaults to tasks."
          },
          "take": { "type": "integer", "description": "How many recent sprints to include. Defaults to 12." }
        }
        """,
        "projectId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.GetInt(arguments, "projectId");

        if (!projectId.HasValue)
        {
            return AiToolExecution.Failed("A projectId is required.");
        }

        var filter = new VelocityFilter
        {
            ProjectId = projectId.Value,
            Unit = AiToolSchema.GetEnum<ReportingUnit>(arguments, "unit") ?? ReportingUnit.Tasks,
            Take = AiToolSchema.GetInt(arguments, "take") ?? DefaultTake,
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
}
