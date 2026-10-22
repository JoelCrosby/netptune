using Mediator;

using Netptune.Core.Models.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPagesNeedingReviewQuery : IRequest<ClientResponse<List<PageNeedingReviewViewModel>>>;

public sealed class GetPagesNeedingReviewQueryHandler
    : IRequestHandler<GetPagesNeedingReviewQuery, ClientResponse<List<PageNeedingReviewViewModel>>>
{
    public const int Take = 6;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPagesNeedingReviewQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<PageNeedingReviewViewModel>>> Handle(
        GetPagesNeedingReviewQuery request,
        CancellationToken cancellationToken)
    {
        var isSignedIn = Identity.IsSignedIn();

        if (!isSignedIn)
        {
            return ClientResponse<List<PageNeedingReviewViewModel>>.Success([]);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var window = PageReviewWindow.At(workspaceId, DateTime.UtcNow, Take);
        var pages = await UnitOfWork.Pages.GetNeedingReview(window, cancellationToken);

        return ClientResponse<List<PageNeedingReviewViewModel>>.Success(pages);
    }
}
