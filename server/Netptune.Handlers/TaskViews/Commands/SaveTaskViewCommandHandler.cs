using System.Text.Json;

using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Query.Tasks;
using Netptune.Query.Validation;
using Netptune.Query.Views;

namespace Netptune.Handlers.TaskViews.Commands;

public sealed record SaveTaskViewRequest
{
    public int? Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public bool IsShared { get; init; }

    public required TaskViewDefinition Definition { get; init; }
}

public sealed record SaveTaskViewCommand(SaveTaskViewRequest Request) : IRequest<ClientResponse<TaskViewViewModel>>;

public sealed class SaveTaskViewCommandHandler : IRequestHandler<SaveTaskViewCommand, ClientResponse<TaskViewViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskViewRepository TaskViews;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly TaskReferenceValidator ReferenceValidator;

    public SaveTaskViewCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ITaskViewRepository taskViews,
        IWorkspacePermissionCache permissionCache,
        TaskReferenceValidator referenceValidator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        TaskViews = taskViews;
        PermissionCache = permissionCache;
        ReferenceValidator = referenceValidator;
    }

    public async ValueTask<ClientResponse<TaskViewViewModel>> Handle(SaveTaskViewCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return ClientResponse<TaskViewViewModel>.Failed("A name is required.");
        }

        var structural = QueryValidator.Validate(TaskFieldCatalog.Instance, input.Definition.Query);

        if (!structural.IsValid)
        {
            return ClientResponse<TaskViewViewModel>.Failed(structural.ToMessage());
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var scope = new QueryWorkspaceScope(workspaceId, workspaceKey);
        var references = await ReferenceValidator.Validate(input.Definition.Query, scope, cancellationToken);

        if (!references.IsValid)
        {
            return ClientResponse<TaskViewViewModel>.Failed(references.ToMessage());
        }

        var name = input.Name.Trim();
        var nameTaken = await TaskViews.NameExists(workspaceId, name, input.Id, cancellationToken);

        if (nameTaken)
        {
            return ClientResponse<TaskViewViewModel>.Failed($"A view named '{name}' already exists.");
        }

        var userId = Identity.GetCurrentUserId();
        var view = await Resolve(input, workspaceId, cancellationToken);

        if (view is null)
        {
            return ClientResponse<TaskViewViewModel>.NotFound;
        }

        var isNew = view.Id == 0;
        var isOwn = isNew || view.CreatedByUserId == userId;
        var canManageShared = await TaskViewPermissions.CanManageShared(PermissionCache, userId, workspaceKey);
        var needsSharedRights = !isOwn || (input.IsShared && !view.IsShared);

        if (needsSharedRights && !canManageShared)
        {
            return ClientResponse<TaskViewViewModel>.Forbidden;
        }

        var display = ClampDisplay(input.Definition.Display);
        var definition = input.Definition with { Version = TaskViewDefinition.CurrentVersion, Display = display };
        view.Name = name;
        view.Description = input.Description;
        view.Icon = input.Icon;
        view.Definition = JsonSerializer.SerializeToDocument(definition, JsonOptions.Default);
        view.IsShared = input.IsShared;
        view.ModifiedByUserId = userId;

        if (isNew)
        {
            view.Slug = name.ToUrlSlug(true);
            view.WorkspaceId = workspaceId;
            view.CreatedByUserId = userId;
            view.OwnerId = userId;

            await TaskViews.AddAsync(view, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = TaskViewMapper.ToViewModel(view, userId, canManageShared);

        return ClientResponse<TaskViewViewModel>.Success(viewModel);
    }

    private static TaskViewDisplay ClampDisplay(TaskViewDisplay display)
    {
        var pageSize = Math.Clamp(display.PageSize, 1, PaginationDefaults.MaxPageSize);

        return display with { PageSize = pageSize };
    }

    private async Task<TaskView?> Resolve(SaveTaskViewRequest input, int workspaceId, CancellationToken cancellationToken)
    {
        if (input.Id is null)
        {
            return new TaskView();
        }

        return await TaskViews.GetInWorkspace(input.Id.Value, workspaceId, cancellationToken: cancellationToken);
    }
}
