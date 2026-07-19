using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Handlers.Projects.Commands;

public sealed record UpdateProjectCommand(UpdateProjectRequest Request) : IRequest<ClientResponse<ProjectViewModel>>;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ClientResponse<ProjectViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public UpdateProjectCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<ProjectViewModel>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await UnitOfWork.Projects.GetWithIncludes(request.Request.Id!.Value, cancellationToken);
        var user = await Identity.GetCurrentUser();

        if (project is null) return ClientResponse<ProjectViewModel>.NotFound;

        project.Name = request.Request.Name ?? project.Name;
        project.Description = request.Request.Description ?? project.Description;
        project.RepositoryUrl = request.Request.RepositoryUrl ?? project.RepositoryUrl;
        project.Key = request.Request.Key ?? project.Key;
        project.ModifiedByUserId = user.Id;

        if (request.Request.DefaultStatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(
                request.Request.DefaultStatusId.Value,
                project.WorkspaceId,
                cancellationToken: cancellationToken);

            if (status is null || status.EntityType != EntityType.Task)
            {
                return ClientResponse<ProjectViewModel>.Failed("Default task status not found");
            }

            project.DefaultStatusId = status.Id;
            project.DefaultStatus = status;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = project.Id;
            options.EntityType = EntityType.Project;
            options.Type = ActivityType.Modify;
        });

        var workspaceKey = Identity.GetWorkspaceKey();

        await EventPublisher.Dispatch(new SearchIndexEvent
        {
            Operation = SearchIndexOperation.Index,
            EntityType = "project",
            EntityIds = [project.Id],
            WorkspaceSlug = workspaceKey,
        });

        return project.ToViewModel();
    }
}
