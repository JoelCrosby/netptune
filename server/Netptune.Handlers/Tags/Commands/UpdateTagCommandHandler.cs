using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Handlers.Tags.Commands;

public sealed record UpdateTagCommand(UpdateTagRequest Request) : IRequest<ClientResponse<TagViewModel>>;

public sealed class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, ClientResponse<TagViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public UpdateTagCommandHandler(
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

    public async ValueTask<ClientResponse<TagViewModel>> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (!workspaceId.HasValue)
        {
            return ClientResponse<TagViewModel>.Failed($"workspace with key {workspaceKey} does not exist");
        }

        var tag = await UnitOfWork.Tags.GetByValue(request.Request.CurrentValue, workspaceId.Value, cancellationToken: cancellationToken);

        if (tag is null)
        {
            return ClientResponse<TagViewModel>.NotFound;
        }

        tag.Name = request.Request.NewValue.Trim();

        await UnitOfWork.CompleteAsync(cancellationToken);

        var taggedPageIds = await UnitOfWork.Pages.GetIdsTaggedWith([tag.Id], workspaceId.Value, cancellationToken);

        if (taggedPageIds.Count > 0)
        {
            await EventPublisher.IndexPages(taggedPageIds, workspaceKey);
        }

        Activity.Log(options =>
        {
            options.EntityId = tag.Id;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.ModifyName;
        });

        return ClientResponse<TagViewModel>.Success(tag.ToViewModel());
    }
}
