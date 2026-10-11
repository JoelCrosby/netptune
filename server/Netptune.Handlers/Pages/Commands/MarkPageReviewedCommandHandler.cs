using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages.Commands;

public sealed record MarkPageReviewedCommand(int Id) : IRequest<ClientResponse>;

public sealed class MarkPageReviewedCommandHandler : IRequestHandler<MarkPageReviewedCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public MarkPageReviewedCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MarkPageReviewedCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var marked = await UnitOfWork.Pages.MarkReviewed(request.Id, workspaceId, userId, cancellationToken);

        if (!marked)
        {
            return ClientResponse.NotFound;
        }

        Activity.Log(options =>
        {
            options.EntityId = request.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse.Success;
    }
}
