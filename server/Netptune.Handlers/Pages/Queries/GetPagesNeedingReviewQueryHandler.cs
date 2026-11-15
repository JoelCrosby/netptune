using Mediator;

using Netptune.Core.Models.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPagesNeedingReviewQuery : IRequest<ClientResponse<PagesNeedingReviewViewModel>>;

public sealed class GetPagesNeedingReviewQueryHandler
    : IRequestHandler<GetPagesNeedingReviewQuery, ClientResponse<PagesNeedingReviewViewModel>>
{
    public const int Take = 6;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPagesNeedingReviewQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagesNeedingReviewViewModel>> Handle(
        GetPagesNeedingReviewQuery request,
        CancellationToken cancellationToken)
    {
        var isSignedIn = Identity.IsSignedIn();

        if (!isSignedIn)
        {
            var empty = new PagesNeedingReviewViewModel { Pages = [] };

            return ClientResponse<PagesNeedingReviewViewModel>.Success(empty);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var window = PageReviewWindow.At(workspaceId, DateTime.UtcNow, Take);
        var pages = await UnitOfWork.Pages.GetNeedingReview(window, cancellationToken);

        return ClientResponse<PagesNeedingReviewViewModel>.Success(pages);
    }
}
