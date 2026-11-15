using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages.Commands;

public sealed record MarkPagesReviewedCommand(MarkPagesReviewedRequest Request) : IRequest<ClientResponse>;

public sealed class MarkPagesReviewedCommandHandler : IRequestHandler<MarkPagesReviewedCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public MarkPagesReviewedCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MarkPagesReviewedCommand request, CancellationToken cancellationToken)
    {
        var ids = request.Request.Ids.Distinct().ToList();

        if (ids.Count == 0)
        {
            return ClientResponse.Success;
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var marked = await UnitOfWork.Pages.MarkReviewed(ids, workspaceId, userId, cancellationToken);

        if (marked.Count == 0)
        {
            return ClientResponse.NotFound;
        }

        Activity.LogMany(options =>
        {
            options.EntityIds = marked;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse.Success;
    }
}
