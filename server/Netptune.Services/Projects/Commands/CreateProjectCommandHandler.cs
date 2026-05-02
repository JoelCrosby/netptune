using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Commands;

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

            var project = Project.Create(new()
            {
                Name = request.Request.Name,
                Description = request.Request.Description,
                Key = projectKey,
                UserId = user.Id,
                WorkspaceId = workspace.Id,
                RepositoryUrl = request.Request.RepositoryUrl,
                MetaInfo = request.Request.MetaInfo,
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
}
