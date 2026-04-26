using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, ClientResponse<Workspace>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateWorkspaceCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<Workspace>> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var result = await UnitOfWork.Workspaces.GetBySlug(request.Request.Slug!);

        if (result is null) return ClientResponse<Workspace>.NotFound;

        result.Name = request.Request.Name ?? result.Name;
        result.Description = request.Request.Description ?? result.Description;
        result.ModifiedByUserId = userId;
        result.MetaInfo = request.Request.MetaInfo ?? result.MetaInfo;
        result.IsPublic = request.Request.IsPublic ?? result.IsPublic;
        result.UpdatedAt = DateTime.UtcNow;

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.WorkspaceId = result.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse<Workspace>.Success(result);
    }
}
