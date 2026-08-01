using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetCurrentSprintTool : IAiTool
{
    private const int MaximumTasks = 100;

    private readonly IMediator Mediator;

    public GetCurrentSprintTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_current_sprint";

    public string Description =>
        "Read the sprint that is running now, with its dates, goal, task counts and the tasks in it.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCurrentSprintQuery(), cancellationToken);
        var sprint = result.Payload;

        if (sprint is null)
        {
            return AiToolExecution.Success("""{"sprint":null}""");
        }

        var content = JsonSerializer.Serialize(new
        {
            sprint = new
            {
                id = sprint.Id,
                name = sprint.Name,
                goal = sprint.Goal,
                status = sprint.Status.ToString(),
                startDate = sprint.StartDate,
                endDate = sprint.EndDate,
                projectId = sprint.ProjectId,
                projectName = sprint.ProjectName,
                newTaskCount = sprint.NewTaskCount,
                activeTaskCount = sprint.ActiveTaskCount,
                doneTaskCount = sprint.DoneTaskCount,
            },
            tasks = sprint.Tasks.Take(MaximumTasks).Select(task => new
            {
                id = task.Id,
                systemId = task.SystemId,
                name = task.Name,
                status = task.StatusName,
                assignees = task.Assignees.Select(assignee => assignee.DisplayName),
            }),
        });

        return AiToolExecution.Success(content);
    }
}
