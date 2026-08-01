using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetTaskTool : IAiTool
{
    private readonly IMediator Mediator;

    public GetTaskTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_task";

    public string Description =>
        "Read one task in full by its systemId, including description, assignees, tags, dates, estimate and flags.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "systemId": { "type": "string", "description": "The task system id, such as NPT-42." }
        }
        """,
        "systemId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId)
        {
            return AiToolExecution.Failed("A systemId is required.");
        }

        var task = await Mediator.Send(new GetTaskDetailQuery(systemId!), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {systemId} was not found in this workspace.");
        }

        var content = JsonSerializer.Serialize(new
        {
            id = task.Id,
            systemId = task.SystemId,
            name = task.Name,
            description = task.Description,
            status = task.StatusName,
            statusId = task.StatusId,
            statusCategory = task.StatusCategory.ToString(),
            assignees = task.Assignees.Select(assignee => new { userId = assignee.Id, assignee.DisplayName }),
            tags = task.Tags,
            priority = task.Priority?.ToString(),
            estimateType = task.EstimateType?.ToString(),
            estimateValue = task.EstimateValue,
            startDate = task.StartDate,
            dueDate = task.DueDate,
            projectId = task.ProjectId,
            projectName = task.ProjectName,
            sprintId = task.SprintId,
            sprintName = task.SprintName,
            boardGroupId = task.BoardGroupId,
            hasComments = task.HasComments,
            flags = task.Flags.Select(flag => flag.Name),
        });

        return AiToolExecution.Success(content);
    }
}
