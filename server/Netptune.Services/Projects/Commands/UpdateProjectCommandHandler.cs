using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Commands;

public sealed record UpdateProjectCommand(UpdateProjectRequest Request) : IRequest<ClientResponse<ProjectViewModel>>;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ClientResponse<ProjectViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateProjectCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<ProjectViewModel>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await UnitOfWork.Projects.GetWithIncludes(request.Request.Id!.Value);
        var user = await Identity.GetCurrentUser();

        if (project is null) return ClientResponse<ProjectViewModel>.NotFound;

        project.Name = request.Request.Name ?? project.Name;
        project.Description = request.Request.Description ?? project.Description;
        project.RepositoryUrl = request.Request.RepositoryUrl ?? project.RepositoryUrl;
        project.Key = request.Request.Key ?? project.Key;
        project.ModifiedByUserId = user.Id;

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = project.Id;
            options.EntityType = EntityType.Project;
            options.Type = ActivityType.Modify;
        });

        return project.ToViewModel();
    }
}
