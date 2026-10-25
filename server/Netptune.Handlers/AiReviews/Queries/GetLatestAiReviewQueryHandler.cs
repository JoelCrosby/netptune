using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.AiReviews.Queries;

public sealed record GetLatestAiReviewQuery(int TaskId) : IRequest<ClientResponse<AiReviewLatestViewModel>>;

public sealed class GetLatestAiReviewQueryHandler
    : IRequestHandler<GetLatestAiReviewQuery, ClientResponse<AiReviewLatestViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetLatestAiReviewQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiReviewLatestViewModel>> Handle(
        GetLatestAiReviewQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var fingerprints = await UnitOfWork.AiReviews.GetTaskSpecFingerprints(
            workspaceId,
            [request.TaskId],
            cancellationToken);
        var taskExists = fingerprints.TryGetValue(request.TaskId, out var currentFingerprint);

        if (!taskExists)
        {
            return ClientResponse<AiReviewLatestViewModel>.NotFound;
        }

        var subject = new AiReviewSubject(EntityType.Task, request.TaskId);
        var runs = await UnitOfWork.AiReviews.GetRuns(subject, workspaceId, cancellationToken);
        var mentions = await UnitOfWork.AiReviews.GetMentions(subject, workspaceId, cancellationToken);
        var origin = await UnitOfWork.AiReviews.GetOrigin(subject, workspaceId, cancellationToken);
        var currentRun = runs.FirstOrDefault(run => run.Status == AiReviewRunStatus.Succeeded);
        var latestRun = runs.FirstOrDefault();
        var latestFailure = latestRun?.Status == AiReviewRunStatus.Failed ? latestRun : null;

        if (currentRun is null)
        {
            return ClientResponse<AiReviewLatestViewModel>.Success(new AiReviewLatestViewModel
            {
                LatestFailure = latestFailure,
                Readiness = AiReviewReadiness.Unreviewed,
                MentionedIn = mentions,
                Origin = origin,
            });
        }

        var findings = await UnitOfWork.AiReviews.GetFindings(currentRun.Id, cancellationToken);
        var isStale = currentRun.BaselineFingerprint != currentFingerprint;
        var readiness = AiReviewReadinessRules.For(isStale, currentRun.OpenCount);

        return ClientResponse<AiReviewLatestViewModel>.Success(new AiReviewLatestViewModel
        {
            Current = new AiReviewRunDetailViewModel { Run = currentRun, Findings = findings },
            LatestFailure = latestFailure,
            IsStale = isStale,
            Readiness = readiness,
            OpenCount = currentRun.OpenCount,
            MentionedIn = mentions,
            Origin = origin,
        });
    }
}
