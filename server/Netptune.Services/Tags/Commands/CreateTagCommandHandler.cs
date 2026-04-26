using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Extensions;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Commands;

public sealed record CreateTagCommand(AddTagRequest Request) : IRequest<ClientResponse<TagViewModel>>;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, ClientResponse<TagViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateTagCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<TagViewModel>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (!workspaceId.HasValue)
        {
            return ClientResponse<TagViewModel>.NotFound;
        }

        var trimmedTag = request.Request.Tag.Trim().Capitalize();
        var alreadyExists = await UnitOfWork.Tags.Exists(trimmedTag, workspaceId.Value);

        if (alreadyExists) return ClientResponse<TagViewModel>.Failed("Tag Name should be unique");

        var tag = new Tag
        {
            Name = request.Request.Tag,
            OwnerId = userId,
            WorkspaceId = workspaceId.Value,
            IsDeleted = false,
        };

        await UnitOfWork.Tags.AddAsync(tag);
        await UnitOfWork.CompleteAsync();

        var result = await UnitOfWork.Tags.GetViewModel(tag.Id);

        Activity.Log(options =>
        {
            options.EntityId = tag.Id;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<TagViewModel>.Success(result!);
    }
}
