using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.AiReviews.Commands;

public sealed record DecideAiReviewFindingCommand(int RunId, long FindingId, DecideAiReviewFindingRequest Request)
    : IRequest<ClientResponse>;

public sealed class DecideAiReviewFindingCommandHandler : IRequestHandler<DecideAiReviewFindingCommand, ClientResponse>
{
    private const int MaxReasonLength = 500;
    private const int LockAttempts = 20;

    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAdvisoryLock AdvisoryLock;
    private readonly IAiExecutionContext AiExecution;
    private readonly IEventRecordWriter EventRecords;
    private readonly IReadOnlyDictionary<EntityType, IAiReviewSubject> Subjects;

    public DecideAiReviewFindingCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAdvisoryLock advisoryLock,
        IAiExecutionContext aiExecution,
        IEventRecordWriter eventRecords,
        IEnumerable<IAiReviewSubject> subjects)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        AdvisoryLock = advisoryLock;
        AiExecution = aiExecution;
        EventRecords = eventRecords;
        Subjects = subjects.ToDictionary(subject => subject.Type);
    }

    public async ValueTask<ClientResponse> Handle(
        DecideAiReviewFindingCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var reason = request.Reason?.Trim();
        var isReasonTooLong = reason?.Length > MaxReasonLength;

        if (isReasonTooLong)
        {
            return ClientResponse.Failed(Localized.Format(AppText.AiReviewReasonTooLong, MaxReasonLength));
        }

        // Deciding reads a field, writes it back and moves the baseline; two decisions on one run must not
        // interleave or one edit overwrites the other.
        await using var runLock = await AcquireRunLock(command.RunId, cancellationToken);

        if (runLock is null)
        {
            return ClientResponse.Failed(AppText.AiReviewBusy);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var run = await UnitOfWork.AiReviews.GetRunForUpdate(command.RunId, workspaceId, cancellationToken);

        if (run is null)
        {
            return ClientResponse.NotFound;
        }

        var finding = await UnitOfWork.AiReviews.GetFindingForUpdate(run.Id, command.FindingId, cancellationToken);

        if (finding is null)
        {
            return ClientResponse.NotFound;
        }

        var hasTargetSource = Subjects.TryGetValue(finding.TargetType, out var target);
        var hasRunSource = Subjects.TryGetValue(run.SubjectType, out var subject);

        if (!hasTargetSource || !hasRunSource)
        {
            return ClientResponse.NotFound;
        }

        var isPermitted = await IsPermitted(target!.DecidePermissions, cancellationToken);

        if (!isPermitted)
        {
            return ClientResponse.Forbidden;
        }

        var isSucceeded = run.Status == AiReviewRunStatus.Succeeded;
        var hasNewerRun = await UnitOfWork.AiReviews.HasNewerSucceededRun(run, cancellationToken);

        if (!isSucceeded || hasNewerRun)
        {
            return ClientResponse.Failed(AppText.AiReviewNotCurrent);
        }

        var decision = new Decision
        {
            Run = run,
            Finding = finding,
            Target = target!,
            Subject = subject!,
            WorkspaceId = workspaceId,
            UserId = Identity.GetCurrentUserId(),
            Reason = reason,
        };

        return request.Decision switch
        {
            AiReviewDecision.Accept => await Accept(decision, request, cancellationToken),
            AiReviewDecision.Dismiss => await Dismiss(decision, cancellationToken),
            AiReviewDecision.Reopen => await Reopen(decision, request.Revert, cancellationToken),
            _ => ClientResponse.Failed(AppText.ErrorUnknownAction),
        };
    }

    private async Task<IAsyncDisposable?> AcquireRunLock(int runId, CancellationToken cancellationToken)
    {
        var key = AdvisoryLockKeys.AiReviewRun(runId);

        for (var attempt = 1; attempt <= LockAttempts; attempt++)
        {
            var acquired = await AdvisoryLock.TryAcquire(key, cancellationToken);

            if (acquired is not null)
            {
                return acquired;
            }

            await Task.Delay(LockRetryDelay, cancellationToken);
        }

        return null;
    }

    private async Task<bool> IsPermitted(IEnumerable<string> permissions, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            userId,
            workspaceKey,
            cancellationToken: cancellationToken);
        var isPermitted = membership is not null && membership.Allows(permissions);

        return isPermitted;
    }

    private async Task<ClientResponse> Dismiss(Decision decision, CancellationToken cancellationToken)
    {
        var finding = decision.Finding;

        if (finding.Status != AiReviewFindingStatus.Open)
        {
            return ClientResponse.Failed(AppText.AiReviewFindingAlreadyDecided);
        }

        finding.Status = AiReviewFindingStatus.Dismissed;
        finding.DismissReason = decision.Reason;
        finding.DecidedByUserId = decision.UserId;
        finding.DecidedAt = DateTime.UtcNow;

        var payload = DecisionPayload(decision) with { Reason = decision.Reason };

        await Record(decision, EventKeys.AssistantReviewFindingDismissed, payload, cancellationToken);

        return ClientResponse.Success;
    }

    private async Task<ClientResponse> Reopen(Decision decision, bool revert, CancellationToken cancellationToken)
    {
        var finding = decision.Finding;

        if (finding.Status == AiReviewFindingStatus.Open)
        {
            return ClientResponse.Success;
        }

        var appliedEdit = ReadAppliedEdit(finding);
        var isAccepted = finding.Status == AiReviewFindingStatus.Accepted;
        var isAppliedStructuralChange = isAccepted && appliedEdit is null;

        if (isAppliedStructuralChange)
        {
            return ClientResponse.Failed(AppText.AiReviewCannotReopenApplied);
        }

        var shouldRevert = revert && isAccepted && appliedEdit is not null;

        if (shouldRevert)
        {
            var reverted = await RevertEdit(decision, appliedEdit!, cancellationToken);

            if (!reverted.IsSuccess)
            {
                return reverted;
            }
        }

        finding.Status = AiReviewFindingStatus.Open;
        finding.DismissReason = null;
        finding.DecidedByUserId = null;
        finding.DecidedAt = null;
        finding.AppliedChange = null;

        var payload = DecisionPayload(decision) with { Reverted = shouldRevert };

        await Record(decision, EventKeys.AssistantReviewFindingReopened, payload, cancellationToken);

        return ClientResponse.Success;
    }

    private async Task<ClientResponse> Accept(
        Decision decision,
        DecideAiReviewFindingRequest request,
        CancellationToken cancellationToken)
    {
        var finding = decision.Finding;

        if (finding.Status != AiReviewFindingStatus.Open)
        {
            return ClientResponse.Failed(AppText.AiReviewFindingAlreadyDecided);
        }

        if (finding.Suggestion is null)
        {
            return ClientResponse.Failed(AppText.AiReviewNothingToAccept);
        }

        var edit = ReadEditSuggestion(finding);

        if (edit is null)
        {
            return await AcceptStructural(decision, request, cancellationToken);
        }

        var replacement = request.Replace ?? edit.Replace;
        var wasEdited = request.Replace is not null && request.Replace != edit.Replace;
        var before = await decision.Target.ReadField(
            decision.WorkspaceId,
            finding.TargetId,
            edit.Field,
            cancellationToken);

        if (before is null)
        {
            return ClientResponse.NotFound;
        }

        var after = AiReviewEdits.Apply(before, edit.Find, replacement);

        if (after is null)
        {
            return ClientResponse.Failed(AppText.AiReviewPassageChanged);
        }

        var written = await WriteAsAssistant(decision, edit.Field, after, cancellationToken);

        if (!written.IsSuccess)
        {
            return written;
        }

        var appliedEdit = new AiReviewAppliedEdit
        {
            Field = edit.Field,
            Before = before,
            After = after,
            Edited = wasEdited,
        };

        finding.Status = AiReviewFindingStatus.Accepted;
        finding.DecidedByUserId = decision.UserId;
        finding.DecidedAt = DateTime.UtcNow;
        finding.AppliedChange = JsonSerializer.SerializeToDocument(appliedEdit, JsonOptions.Default);

        await MoveBaseline(decision, cancellationToken);

        var change = new AssistantReviewAppliedEntity
        {
            Kind = "fieldEdit",
            EntityType = EventEntityTypes.From(finding.TargetType),
            EntityId = finding.TargetId,
        };

        var payload = DecisionPayload(decision) with { Edited = wasEdited, Changes = [change] };

        await Record(decision, EventKeys.AssistantReviewFindingAccepted, payload, cancellationToken);

        return ClientResponse.Success;
    }

    private async Task<ClientResponse> AcceptStructural(
        Decision decision,
        DecideAiReviewFindingRequest request,
        CancellationToken cancellationToken)
    {
        var finding = decision.Finding;
        var applyRequest = new AiReviewApplyRequest
        {
            WorkspaceId = decision.WorkspaceId,
            TargetId = finding.TargetId,
            Suggestion = finding.Suggestion!.RootElement,
            Decision = request,
        };

        var extraPermissions = decision.Target.PermissionsFor(applyRequest);
        var isPermitted = await IsPermitted(extraPermissions, cancellationToken);

        if (!isPermitted)
        {
            return ClientResponse.Forbidden;
        }

        var application = await ApplyAsAssistant(decision, applyRequest, cancellationToken);

        if (!application.IsSuccess)
        {
            return ClientResponse.Failed(application.Message);
        }

        finding.Status = AiReviewFindingStatus.Accepted;
        finding.DecidedByUserId = decision.UserId;
        finding.DecidedAt = DateTime.UtcNow;
        finding.AppliedChange = application.AppliedChange;

        foreach (var created in application.Created)
        {
            finding.References.Add(new AiReviewFindingReference
            {
                Role = AiReviewReferenceRole.Created,
                EntityType = created.EntityType,
                EntityId = created.EntityId,
            });
        }

        await MoveBaseline(decision, cancellationToken);

        var payload = DecisionPayload(decision) with { Changes = application.Changes };

        await Record(decision, EventKeys.AssistantReviewFindingAccepted, payload, cancellationToken);

        return ClientResponse.Success;
    }

    private async Task<AiReviewApplication> ApplyAsAssistant(
        Decision decision,
        AiReviewApplyRequest request,
        CancellationToken cancellationToken)
    {
        using (AiExecution.Begin(decision.Run.Model, decision.Run.CorrelationId))
        {
            return await decision.Target.ApplySuggestion(request, cancellationToken);
        }
    }

    private async Task<ClientResponse> RevertEdit(
        Decision decision,
        AiReviewAppliedEdit appliedEdit,
        CancellationToken cancellationToken)
    {
        var finding = decision.Finding;
        var current = await decision.Target.ReadField(
            decision.WorkspaceId,
            finding.TargetId,
            appliedEdit.Field,
            cancellationToken);
        var isUnchangedSinceAccept = current == appliedEdit.After;

        if (!isUnchangedSinceAccept)
        {
            return ClientResponse.Failed(AppText.AiReviewRevertChanged);
        }

        var written = await WriteAsAssistant(decision, appliedEdit.Field, appliedEdit.Before, cancellationToken);

        if (!written.IsSuccess)
        {
            return written;
        }

        await MoveBaseline(decision, cancellationToken);

        return ClientResponse.Success;
    }

    private async Task<ClientResponse> WriteAsAssistant(
        Decision decision,
        string field,
        string value,
        CancellationToken cancellationToken)
    {
        using (AiExecution.Begin(decision.Run.Model, decision.Run.CorrelationId))
        {
            return await decision.Target.WriteField(decision.Finding.TargetId, field, value, cancellationToken);
        }
    }

    private async Task MoveBaseline(Decision decision, CancellationToken cancellationToken)
    {
        var run = decision.Run;
        var fingerprint = await decision.Subject.GetFingerprint(decision.WorkspaceId, run.SubjectId, cancellationToken);

        if (fingerprint is not null)
        {
            run.BaselineFingerprint = fingerprint;
        }
    }

    private async Task Record(
        Decision decision,
        string eventKey,
        AssistantReviewFindingDecidedPayload payload,
        CancellationToken cancellationToken)
    {
        var finding = decision.Finding;
        var decisionEvent = new EventWriteRequest<AssistantReviewFindingDecidedPayload>
        {
            WorkspaceId = decision.WorkspaceId,
            EventKey = eventKey,
            SubjectType = EventEntityTypes.From(finding.TargetType),
            SubjectId = finding.TargetId.ToString(),
            CorrelationId = decision.Run.CorrelationId,
            Payload = payload,
        };

        await EventRecords.Append(decisionEvent, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private static AssistantReviewFindingDecidedPayload DecisionPayload(Decision decision)
    {
        var finding = decision.Finding;

        return new AssistantReviewFindingDecidedPayload
        {
            RunId = decision.Run.Id,
            FindingId = finding.Id,
            Category = finding.Category.ToString(),
            Title = finding.Title,
        };
    }

    private static AiReviewEditSuggestion? ReadEditSuggestion(AiReviewFinding finding)
    {
        if (finding.Suggestion is null)
        {
            return null;
        }

        var suggestion = finding.Suggestion.RootElement;
        var isEdit = AiReviewSuggestionKinds.Read(suggestion) == AiReviewSuggestionKinds.Edit;

        return isEdit ? suggestion.Deserialize<AiReviewEditSuggestion>(JsonOptions.Default) : null;
    }

    private static AiReviewAppliedEdit? ReadAppliedEdit(AiReviewFinding finding)
    {
        if (finding.AppliedChange is null)
        {
            return null;
        }

        var applied = finding.AppliedChange.RootElement;
        var isFieldEdit = applied.TryGetProperty("before", out _);

        return isFieldEdit ? applied.Deserialize<AiReviewAppliedEdit>(JsonOptions.Default) : null;
    }

    private sealed record Decision
    {
        public required AiReviewRun Run { get; init; }

        public required AiReviewFinding Finding { get; init; }

        public required IAiReviewSubject Target { get; init; }

        public required IAiReviewSubject Subject { get; init; }

        public int WorkspaceId { get; init; }

        public required string UserId { get; init; }

        public string? Reason { get; init; }
    }
}
