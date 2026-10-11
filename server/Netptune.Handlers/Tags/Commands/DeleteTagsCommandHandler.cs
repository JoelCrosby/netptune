using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tags.Commands;

public sealed record DeleteTagsCommand(DeleteTagsRequest Request) : IRequest<ClientResponse>;

public sealed class DeleteTagsCommandHandler : IRequestHandler<DeleteTagsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public DeleteTagsCommandHandler(
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

    public async ValueTask<ClientResponse> Handle(DeleteTagsCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.Failed();

        var tags = await UnitOfWork.Tags.GetTagsByValueInWorkspace(workspaceId.Value, request.Request.Tags, cancellationToken: cancellationToken);
        var tagIds = tags.ConvertAll(t => t.Id);

        // Read before the delete takes the join rows with it; pages index their tags by name.
        var taggedPageIds = await UnitOfWork.Pages.GetIdsTaggedWith(tagIds, workspaceId.Value, cancellationToken);

        await UnitOfWork.Tags.DeletePermanent(tags);
        await UnitOfWork.CompleteAsync(cancellationToken);

        if (taggedPageIds.Count > 0)
        {
            await EventPublisher.IndexPages(taggedPageIds, workspaceKey);
        }

        Activity.LogMany(options =>
        {
            options.EntityIds = tagIds;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
