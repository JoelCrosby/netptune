using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai;

public static class AiChangeRouteIdReader
{
    public static async Task<AiChangeRouteIds> Read(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyCollection<AiProposedChange> changes,
        CancellationToken cancellationToken)
    {
        var taskIds = AiChangeRouteIds.CollectIds(changes, AiChangeRouteIds.Task);
        var projectIds = AiChangeRouteIds.CollectIds(changes, AiChangeRouteIds.Project);
        var boardIds = AiChangeRouteIds.CollectIds(changes, AiChangeRouteIds.Board);
        var sprintIds = AiChangeRouteIds.CollectIds(changes, AiChangeRouteIds.Sprint);

        var tasks = await unitOfWork.Tasks.GetTaskViewModels(taskIds, cancellationToken);
        var projects = projectIds.Count == 0 ? [] : await unitOfWork.Projects.GetAllByIdAsync(projectIds, true, cancellationToken);
        var boards = boardIds.Count == 0 ? [] : await unitOfWork.Boards.GetAllByIdAsync(boardIds, true, cancellationToken);
        var sprints = sprintIds.Count == 0 ? [] : await unitOfWork.Sprints.GetAllByIdAsync(sprintIds, true, cancellationToken);

        return new AiChangeRouteIds
        {
            Tasks = tasks.ToDictionary(task => task.Id, task => task.SystemId),
            Projects = projects.ToDictionary(project => project.Id, project => project.Key),
            Boards = boards.ToDictionary(board => board.Id, board => board.Identifier),
            Sprints = sprints.ToDictionary(sprint => sprint.Id, sprint => sprint.Identifier),
        };
    }
}
