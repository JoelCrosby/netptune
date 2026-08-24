using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pins;

namespace Netptune.Handlers.Pins;

public sealed record PinProjectionScope
{
    public required int WorkspaceId { get; init; }

    public required string UserId { get; init; }

    public required PinWriteRights Rights { get; init; }
}

public static class PinnedTaskProjection
{
    public static async Task<List<PinnedTaskViewModel>> Build(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyCollection<TaskPin> pins,
        PinProjectionScope scope,
        CancellationToken cancellationToken)
    {
        if (pins.Count == 0)
        {
            return [];
        }

        var candidateTaskIds = pins.Select(pin => pin.ProjectTaskId).Distinct().ToList();
        var readableTaskIds = await unitOfWork.Tasks.GetValidTaskIdsInWorkspace(candidateTaskIds, scope.WorkspaceId, cancellationToken);
        var readable = readableTaskIds.ToHashSet();
        var visiblePins = pins.Where(pin => readable.Contains(pin.ProjectTaskId)).ToList();

        if (visiblePins.Count == 0)
        {
            return [];
        }

        var names = await ResolveScopeNames(unitOfWork, visiblePins, scope.WorkspaceId, cancellationToken);
        var tasks = await unitOfWork.Tasks.GetTaskViewModels(readableTaskIds, cancellationToken);
        var tasksById = tasks.ToDictionary(task => task.Id);

        return visiblePins
            .Where(pin => tasksById.ContainsKey(pin.ProjectTaskId))
            .GroupBy(pin => pin.ProjectTaskId)
            .OrderBy(group => group.Min(pin => pin.SortOrder))
            .ThenByDescending(group => group.Max(pin => pin.CreatedAt))
            .Select(group => new PinnedTaskViewModel
            {
                Task = tasksById[group.Key],
                Pins = group
                    .OrderBy(pin => pin.Scope)
                    .Select(pin => ToViewModel(pin, names, scope))
                    .ToList(),
            })
            .ToList();
    }

    public static TaskPinViewModel ToViewModel(TaskPin pin, ScopeNameLookup names, PinProjectionScope scope)
    {
        var isOwnPersonalPin = pin.Scope == TaskPinScope.User && pin.CreatedByUserId == scope.UserId;

        return new TaskPinViewModel
        {
            Id = pin.Id,
            TaskId = pin.ProjectTaskId,
            Scope = pin.Scope,
            ScopeEntityId = pin.ScopeEntityId,
            ScopeName = names.Resolve(pin.Scope, pin.ScopeEntityId),
            SortOrder = pin.SortOrder,
            CanUnpin = isOwnPersonalPin || scope.Rights.For(pin.Scope),
            CreatedAt = pin.CreatedAt,
            CreatedByUserId = pin.CreatedByUserId,
        };
    }

    public static async Task<ScopeNameLookup> ResolveScopeNames(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyCollection<TaskPin> pins,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var boardIds = ScopeIds(pins, TaskPinScope.Board);
        var projectIds = ScopeIds(pins, TaskPinScope.Project);
        var boards = await unitOfWork.Boards.GetAllByIdAsync(boardIds, true, cancellationToken);
        var projects = await unitOfWork.Projects.GetAllByIdAsync(projectIds, true, cancellationToken);
        var workspace = await unitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);

        return new ScopeNameLookup
        {
            Boards = boards.ToDictionary(board => board.Id, board => board.Name),
            Projects = projects.ToDictionary(project => project.Id, project => project.Name),
            WorkspaceName = workspace?.Name ?? string.Empty,
        };
    }

    private static List<int> ScopeIds(IReadOnlyCollection<TaskPin> pins, TaskPinScope scope)
    {
        return pins
            .Where(pin => pin.Scope == scope)
            .Select(pin => pin.ScopeEntityId)
            .Distinct()
            .ToList();
    }
}

public sealed record ScopeNameLookup
{
    public required Dictionary<int, string> Boards { get; init; }

    public required Dictionary<int, string> Projects { get; init; }

    public required string WorkspaceName { get; init; }

    public string Resolve(TaskPinScope scope, int scopeEntityId) => scope switch
    {
        TaskPinScope.Board => Boards.GetValueOrDefault(scopeEntityId, string.Empty),
        TaskPinScope.Project => Projects.GetValueOrDefault(scopeEntityId, string.Empty),
        _ => WorkspaceName,
    };
}
