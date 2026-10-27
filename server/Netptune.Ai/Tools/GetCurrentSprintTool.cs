using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Sprints;
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
        "Read the sprint that is running now, with its dates, goal, task counts and the tasks in it. "
        + "Each project runs its own sprint: pass projectId to read that project's. Without one, the single "
        + "running sprint is returned in full, or, when several projects have one, a list of them to choose from.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "projectId": { "type": "integer", "description": "Read the running sprint of this project." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.GetInt(arguments, "projectId");
        var running = await Mediator.Send(
            new GetSprintsQuery(projectId, [SprintStatus.Active], Take: null),
            cancellationToken);

        if (running.Count == 0)
        {
            return AiToolExecution.Success("""{"sprint":null}""");
        }

        if (running.Count > 1)
        {
            return AiToolExecution.Success(DescribeRunning(running));
        }

        var sprint = await AiSprintLookup.Find(Mediator, running[0].Id, cancellationToken);

        if (sprint is null)
        {
            return AiToolExecution.Success("""{"sprint":null}""");
        }

        return AiToolExecution.Success(DescribeSprint(sprint));
    }

    private static string DescribeRunning(List<SprintViewModel> running)
    {
        return JsonSerializer.Serialize(new
        {
            note = "Several projects have a sprint running. Pass projectId to read one in full.",
            sprints = running.Select(sprint => new
            {
                id = sprint.Id,
                name = sprint.Name,
                projectId = sprint.ProjectId,
                projectName = sprint.ProjectName,
                startDate = sprint.StartDate,
                endDate = sprint.EndDate,
            }),
        });
    }

    private static string DescribeSprint(SprintDetailViewModel sprint)
    {
        return JsonSerializer.Serialize(new
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
            taskCount = sprint.Tasks.Count,
            tasksTruncated = sprint.Tasks.Count > MaximumTasks,
            tasks = sprint.Tasks.Take(MaximumTasks).Select(task => new
            {
                taskId = task.Id,
                systemId = task.SystemId,
                name = task.Name,
                status = task.StatusName,
                assignees = task.Assignees.Select(assignee => assignee.DisplayName),
            }),
        });
    }
}
