using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Core.Requests;

namespace Netptune.Services.Tags.Commands;

public sealed record UpdateTagCommand(UpdateTagRequest Request) : IRequest<ClientResponse<TagViewModel>>;

public sealed class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, ClientResponse<TagViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateTagCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<TagViewModel>> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (!workspaceId.HasValue)
        {
            return ClientResponse<TagViewModel>.Failed($"workspace with key {workspaceKey} does not exist");
        }

        var tag = await UnitOfWork.Tags.GetByValue(request.Request.CurrentValue, workspaceId.Value);

        if (tag is null)
        {
            return ClientResponse<TagViewModel>.NotFound;
        }

        tag.Name = request.Request.NewValue.Trim();

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = tag.Id;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.ModifyName;
        });

        return ClientResponse<TagViewModel>.Success(tag.ToViewModel());
    }
}
