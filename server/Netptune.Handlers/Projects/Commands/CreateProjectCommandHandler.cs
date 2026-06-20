using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Handlers.Projects.Commands;

public sealed record CreateProjectCommand(AddProjectRequest Request) : IRequest<ClientResponse<ProjectViewModel>>;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ClientResponse<ProjectViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateProjectCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<ProjectViewModel>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        return await UnitOfWork.Transaction(async () =>
        {
            var workspaceKey = Identity.GetWorkspaceKey();
            var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, cancellationToken: cancellationToken);

            if (workspace is null) return ClientResponse<ProjectViewModel>.NotFound;

            var user = await Identity.GetCurrentUser();
            var projectKey = await UnitOfWork.Projects.GenerateProjectKey(request.Request.Name, workspace.Id, cancellationToken);

            await UnitOfWork.Statuses.EnsureDefaultTaskStatuses(workspace.Id, user.Id, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var defaultStatus = await ResolveDefaultStatus(request.Request.DefaultStatusId, workspace.Id, cancellationToken);
            if (defaultStatus is null) return ClientResponse<ProjectViewModel>.Failed("Default task status not found");

            var project = Project.Create(new()
            {
                Name = request.Request.Name,
                Description = request.Request.Description,
                Key = projectKey,
                UserId = user.Id,
                WorkspaceId = workspace.Id,
                RepositoryUrl = request.Request.RepositoryUrl,
                MetaInfo = request.Request.MetaInfo,
                DefaultStatusId = defaultStatus.Id,
            });

            workspace.Projects.Add(project);

            await UnitOfWork.CompleteAsync(cancellationToken);

            var result = await UnitOfWork.Projects.GetProjectViewModel(project.Id, cancellationToken);

            if (result is null) return ClientResponse<ProjectViewModel>.Failed("Project not found");

            Activity.Log(options =>
            {
                options.EntityId = project.Id;
                options.EntityType = EntityType.Project;
                options.Type = ActivityType.Create;
            });

            return result;
        });
    }

    private async Task<Status?> ResolveDefaultStatus(int? statusId, int workspaceId, CancellationToken cancellationToken)
    {
        if (statusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(statusId.Value, workspaceId, cancellationToken: cancellationToken);
            return status?.EntityType == EntityType.Task ? status : null;
        }

        return await UnitOfWork.Statuses.GetTaskStatusByKey(workspaceId, "new", cancellationToken)
               ?? await UnitOfWork.Statuses.GetFirstTaskStatus(workspaceId, cancellationToken);
    }
}
