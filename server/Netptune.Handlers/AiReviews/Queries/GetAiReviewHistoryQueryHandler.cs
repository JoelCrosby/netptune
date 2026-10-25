using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.AiReviews.Queries;

public sealed record GetAiReviewHistoryQuery(int TaskId) : IRequest<ClientResponse<List<AiReviewRunViewModel>>>;

public sealed class GetAiReviewHistoryQueryHandler
    : IRequestHandler<GetAiReviewHistoryQuery, ClientResponse<List<AiReviewRunViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiReviewHistoryQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<AiReviewRunViewModel>>> Handle(
        GetAiReviewHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var subject = new AiReviewSubject(EntityType.Task, request.TaskId);
        var runs = await UnitOfWork.AiReviews.GetRuns(subject, workspaceId, cancellationToken);

        return ClientResponse<List<AiReviewRunViewModel>>.Success(runs);
    }
}
