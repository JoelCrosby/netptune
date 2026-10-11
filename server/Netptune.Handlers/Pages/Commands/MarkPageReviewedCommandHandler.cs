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
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.Id, workspaceId, false, cancellationToken);

        if (page is null)
        {
            return ClientResponse.NotFound;
        }

        page.ReviewedAt = DateTime.UtcNow;
        page.ReviewedByUserId = userId;

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse.Success;
    }
}
