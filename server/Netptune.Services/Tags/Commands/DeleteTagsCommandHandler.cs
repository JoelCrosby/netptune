using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tags.Commands;

public sealed record DeleteTagsCommand(DeleteTagsRequest Request) : IRequest<ClientResponse>;

public sealed class DeleteTagsCommandHandler : IRequestHandler<DeleteTagsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteTagsCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTagsCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.Failed();

        var tags = await UnitOfWork.Tags.GetTagsByValueInWorkspace(workspaceId.Value, request.Request.Tags, cancellationToken: cancellationToken);
        var tagIds = tags.ConvertAll(t => t.Id);

        await UnitOfWork.Tags.DeletePermanent(tags);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = tagIds;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
