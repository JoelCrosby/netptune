using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pins;

namespace Netptune.Handlers.Pins.Commands;

public sealed record CreateTaskPinRequest
{
    public required int TaskId { get; init; }

    public required TaskPinScope Scope { get; init; }

    public int? ScopeEntityId { get; init; }
}

public sealed record CreateTaskPinCommand(CreateTaskPinRequest Request) : IRequest<ClientResponse<TaskPinViewModel>>;

public sealed class CreateTaskPinCommandHandler : IRequestHandler<CreateTaskPinCommand, ClientResponse<TaskPinViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public CreateTaskPinCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        ITaskPinRepository taskPins,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        TaskPins = taskPins;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse<TaskPinViewModel>> Handle(CreateTaskPinCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var userId = Identity.TryGetCurrentUserId();

        if (userId is null)
        {
            return ClientResponse<TaskPinViewModel>.Forbidden;
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var task = await UnitOfWork.Tasks.GetInWorkspace(input.TaskId, workspaceId, true, cancellationToken);

        if (task is null || task.IsDeleted)
        {
            return ClientResponse<TaskPinViewModel>.NotFound;
        }

        var scopeEntityId = ResolveScopeEntityId(input, task, workspaceId);

        if (scopeEntityId is null)
        {
            return ClientResponse<TaskPinViewModel>.Failed("A board is required to pin to a board.");
        }

        var targetExists = await ScopeTargetExists(input.Scope, scopeEntityId.Value, workspaceId, cancellationToken);

        if (!targetExists)
        {
            return ClientResponse<TaskPinViewModel>.NotFound;
        }

        var workspaceKey = Identity.TryGetWorkspaceKey();
        var canWrite = await PinsPermissions.CanWrite(PermissionCache, userId, workspaceKey, input.Scope);

        if (!canWrite)
        {
            return ClientResponse<TaskPinViewModel>.Forbidden;
        }

        var pin = await Resolve(input.Scope, scopeEntityId.Value, task.Id, workspaceId, userId, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var rights = await PinsPermissions.GetWriteRights(PermissionCache, userId, workspaceKey);
        var names = await PinnedTaskProjection.ResolveScopeNames(UnitOfWork, [pin], workspaceId, cancellationToken);
        var scope = new PinProjectionScope
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Rights = rights,
        };
        var viewModel = PinnedTaskProjection.ToViewModel(pin, names, scope);

        return ClientResponse<TaskPinViewModel>.Success(viewModel);
    }

    // A soft-deleted pin is revived rather than replaced: the partial unique indexes only cover live
    // rows, so blind inserts would pile up tombstoned duplicates.
    private async Task<TaskPin> Resolve(
        TaskPinScope scope,
        int scopeEntityId,
        int taskId,
        int workspaceId,
        string userId,
        CancellationToken cancellationToken)
    {
        var existing = await TaskPins.Find(taskId, scope, scopeEntityId, userId, cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            return existing;
        }

        var sortOrder = await TaskPins.GetNextSortOrder(workspaceId, scope, scopeEntityId, cancellationToken);

        if (existing is not null)
        {
            existing.IsDeleted = false;
            existing.DeletedByUserId = null;
            existing.ModifiedByUserId = userId;
            existing.SortOrder = sortOrder;

            return existing;
        }

        var pin = new TaskPin
        {
            ProjectTaskId = taskId,
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            SortOrder = sortOrder,
            WorkspaceId = workspaceId,
            CreatedByUserId = userId,
            OwnerId = userId,
        };

        return await TaskPins.AddAsync(pin, cancellationToken);
    }

    private static int? ResolveScopeEntityId(CreateTaskPinRequest input, ProjectTask task, int workspaceId) => input.Scope switch
    {
        TaskPinScope.User => workspaceId,
        TaskPinScope.Workspace => workspaceId,
        TaskPinScope.Project => input.ScopeEntityId ?? task.ProjectId,
        _ => input.ScopeEntityId,
    };

    private async Task<bool> ScopeTargetExists(TaskPinScope scope, int scopeEntityId, int workspaceId, CancellationToken cancellationToken)
    {
        if (scope == TaskPinScope.Board)
        {
            var board = await UnitOfWork.Boards.GetInWorkspace(scopeEntityId, workspaceId, true, cancellationToken);

            return board is not null && !board.IsDeleted;
        }

        if (scope == TaskPinScope.Project)
        {
            var project = await UnitOfWork.Projects.GetInWorkspace(scopeEntityId, workspaceId, true, cancellationToken);

            return project is not null && !project.IsDeleted;
        }

        return scopeEntityId == workspaceId;
    }
}
