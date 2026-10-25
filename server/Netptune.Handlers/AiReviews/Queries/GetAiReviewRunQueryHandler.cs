using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.AiReviews.Queries;

public sealed record GetAiReviewRunQuery(int TaskId, int RunId) : IRequest<ClientResponse<AiReviewRunDetailViewModel>>;

public sealed class GetAiReviewRunQueryHandler
    : IRequestHandler<GetAiReviewRunQuery, ClientResponse<AiReviewRunDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiReviewRunQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiReviewRunDetailViewModel>> Handle(
        GetAiReviewRunQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var subject = new AiReviewSubject(EntityType.Task, request.TaskId);
        var run = await UnitOfWork.AiReviews.GetRun(request.RunId, subject, workspaceId, cancellationToken);

        if (run is null)
        {
            return ClientResponse<AiReviewRunDetailViewModel>.NotFound;
        }

        var findings = await UnitOfWork.AiReviews.GetFindings(run.Id, cancellationToken);

        return ClientResponse<AiReviewRunDetailViewModel>.Success(new AiReviewRunDetailViewModel
        {
            Run = run,
            Findings = findings,
        });
    }
}
