using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Extensions.Logging;

using Netptune.Ai.Providers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Ai;

namespace Netptune.Ai.Execution.Reviews;

public sealed class AiReviewer : IAiReviewer
{
    private const int MaximumOutputTokens = 32_000;
    private const int ProgressEveryCharacters = 200;
    private const int MaximumStoredReplyCharacters = 8_000;
    private const int MaximumFailureDetailLength = 1_000;

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;
    private readonly IAiSpendService Spend;
    private readonly IAiChatProviderFactory ProviderFactory;
    private readonly IEventRecordWriter EventRecords;
    private readonly IReadOnlyDictionary<EntityType, IAiReviewSubject> Subjects;
    private readonly ILogger<AiReviewer> Logger;

    public AiReviewer(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector,
        IAiSpendService spend,
        IAiChatProviderFactory providerFactory,
        IEventRecordWriter eventRecords,
        IEnumerable<IAiReviewSubject> subjects,
        ILogger<AiReviewer> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
        Spend = spend;
        ProviderFactory = providerFactory;
        EventRecords = eventRecords;
        Subjects = subjects.ToDictionary(subject => subject.Type);
        Logger = logger;
    }

    public async IAsyncEnumerable<AiReviewStreamEvent> Review(
        AiReviewSubject subject,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var hasSource = Subjects.TryGetValue(subject.Type, out var source);

        if (!hasSource)
        {
            yield return AiReviewStreamEvent.Failed(AppText.AiReviewSubjectNotSupported);

            yield break;
        }

        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            userId,
            workspaceKey,
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            yield return AiReviewStreamEvent.Failed(AppText.AiErrorNotWorkspaceMember);

            yield break;
        }

        var isPermitted = membership.Allows(source!.RunPermissions);

        if (!isPermitted)
        {
            yield return AiReviewStreamEvent.Failed(AppText.AiErrorNoPermissionToReview);

            yield break;
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);
        var assistantEnabled = workspace?.AssistantEnabled == true;

        if (!assistantEnabled)
        {
            yield return AiReviewStreamEvent.Failed(AppText.AiErrorAssistantTurnedOff);

            yield break;
        }

        var spend = await Spend.GetStatus(workspace!, cancellationToken);

        if (spend.IsOverCap)
        {
            yield return AiReviewStreamEvent.Failed(AppText.AiSpendCapReached);

            yield break;
        }

        var credential = await AiHandlerCredentials.Resolve(UnitOfWork, userId, workspaceId, cancellationToken);

        if (credential is null)
        {
            yield return AiReviewStreamEvent.Failed(AppText.ErrorNoAssistantKeySetUpWorkspace);

            yield break;
        }

        var loadRequest = new AiReviewLoadRequest
        {
            WorkspaceId = workspaceId,
            SubjectId = subject.Id,
        };

        var context = await source.Load(loadRequest, cancellationToken);

        if (context is null)
        {
            yield return AiReviewStreamEvent.Failed(source.MissingMessage);

            yield break;
        }

        var dismissed = await UnitOfWork.AiReviews.GetDismissals(subject, workspaceId, cancellationToken);
        var startedAt = DateTime.UtcNow;
        var provider = ProviderFactory.Resolve(credential.Provider);
        var apiKey = Protector.Unprotect(credential.Secret);
        var chatRequest = new AiChatRequest
        {
            Model = AiModels.ResolveReviewModel(credential.Provider, credential.ReviewModel),
            SystemPrompt = AiReviewPrompt.SystemPrompt,
            Messages = [new AiChatMessage { Role = AiMessageRole.User, Text = BuildPrompt(context, dismissed) }],
            Tools = [],
            MaxOutputTokens = MaximumOutputTokens,
        };

        var outcome = new ReviewOutcome();

        using var timeout = new CancellationTokenSource(Timeout);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var stream = StreamReply(provider, chatRequest, apiKey, outcome, cancellation.Token);
        var events = AiTurnReader.Read(stream, Timeout, timeout.Token, cancellation.Token);
        var origin = new AiUsageOrigin(workspaceId, userId, context.UsagePurpose);
        var reportedCharacters = 0;

        // Only a finally block still runs when the caller stops enumerating, and the tokens were spent either way.
        try
        {
            await foreach (var streamEvent in events)
            {
                var isFailure = streamEvent.Type == AiStreamEventType.Error;

                if (isFailure)
                {
                    outcome.Failure = streamEvent.Message ?? AppText.AiErrorProviderUnreachable;

                    continue;
                }

                var isDueProgress = outcome.Characters - reportedCharacters >= ProgressEveryCharacters;

                if (isDueProgress)
                {
                    reportedCharacters = outcome.Characters;

                    yield return AiReviewStreamEvent.Progress(outcome.Characters);
                }
            }
        }
        finally
        {
            await BookUsage(origin, credential, chatRequest, outcome, cancellation.IsCancellationRequested);
        }

        var run = new RunRecord
        {
            Subject = subject,
            WorkspaceId = workspaceId,
            UserId = userId,
            Context = context,
            Provider = credential.Provider,
            Model = chatRequest.Model,
            StartedAt = startedAt,
        };

        var failure = ResolveFailure(outcome, timeout.IsCancellationRequested);

        if (failure is not null)
        {
            var failedRun = await StoreFailedRun(run, failure, outcome);
            var message = DescribeFailure(failure.Code, outcome);

            yield return AiReviewStreamEvent.Failed(message, failedRun.Id);

            yield break;
        }

        var parsed = AiReviewReply.Parse(outcome.Turn!.Text);

        if (parsed.Reply is null)
        {
            var unreadable = new ReviewFailure(AiReviewErrorCodes.UnreadableReply, parsed.Problem);
            var unreadableRun = await StoreFailedRun(run, unreadable, outcome);

            yield return AiReviewStreamEvent.Failed(AppText.AiReviewUnreadable, unreadableRun.Id);

            yield break;
        }

        var draft = AiReviewValidator.Validate(parsed.Reply, context, source.Categories);
        var storedRun = await StoreSucceededRun(run, draft);

        yield return AiReviewStreamEvent.Completed(storedRun.Id);
    }

    private static ReviewFailure? ResolveFailure(ReviewOutcome outcome, bool hasTimedOut)
    {
        if (hasTimedOut)
        {
            var timedOutDetail = $"No completed reply after {Timeout.TotalMinutes} minutes.";

            return new ReviewFailure(AiReviewErrorCodes.TimedOut, timedOutDetail);
        }

        if (outcome.Failure is not null)
        {
            return new ReviewFailure(AiReviewErrorCodes.ProviderError, outcome.Failure);
        }

        if (outcome.Turn is null)
        {
            return new ReviewFailure(AiReviewErrorCodes.ProviderError, "The stream ended without a completed turn.");
        }

        var finishReason = outcome.Turn.FinishReason;
        var hitOutputLimit = AiFinishReasons.IsOutputLimit(finishReason);

        if (hitOutputLimit)
        {
            return new ReviewFailure(AiReviewErrorCodes.OutputLimit, null);
        }

        var wasRefused = AiFinishReasons.IsRefusal(finishReason);

        if (wasRefused)
        {
            return new ReviewFailure(AiReviewErrorCodes.Refused, null);
        }

        var isEmpty = string.IsNullOrWhiteSpace(outcome.Turn.Text);

        return isEmpty ? new ReviewFailure(AiReviewErrorCodes.EmptyReply, null) : null;
    }

    private static string DescribeFailure(string failureCode, ReviewOutcome outcome)
    {
        if (outcome.Failure is not null)
        {
            return outcome.Failure;
        }

        return failureCode switch
        {
            AiReviewErrorCodes.OutputLimit => AppText.AiReviewHitOutputLimit,
            AiReviewErrorCodes.Refused => AppText.AiReviewRefused,
            AiReviewErrorCodes.EmptyReply => AppText.AiReviewEmptyReply,
            _ => AppText.AiReviewUnreadable,
        };
    }

    private async Task<AiReviewRun> StoreFailedRun(RunRecord record, ReviewFailure failure, ReviewOutcome outcome)
    {
        var finishReason = outcome.Turn?.FinishReason;
        var detail = DescribeFailureDetail(failure, finishReason);
        var replyText = outcome.Turn?.Text;
        var hasReplyText = !string.IsNullOrEmpty(replyText);
        var rawReply = hasReplyText ? AiReviewPrompt.Truncate(replyText!, MaximumStoredReplyCharacters) : null;

        var failureRecord = new FailureRecord
        {
            Code = failure.Code,
            Detail = AiReviewPrompt.Truncate(detail, MaximumFailureDetailLength),
            RawReply = rawReply,
        };

        var run = CreateRun(record, AiReviewRunStatus.Failed, summary: null, failureRecord);

        await StoreRun(record, run);

        Logger.LogWarning(
            "Assistant review run {RunId} for {SubjectType} {SubjectId} failed with {ErrorCode} using {Provider} {Model} after {Characters} characters: {FailureDetail}",
            run.Id,
            record.Subject.Type,
            record.Subject.Id,
            failure.Code,
            record.Provider,
            record.Model,
            outcome.Characters,
            failureRecord.Detail);

        return run;
    }

    private static string DescribeFailureDetail(ReviewFailure failure, string? finishReason)
    {
        var finishReasonText = $"Finish reason: {finishReason ?? "none"}.";

        return failure.Detail is null ? finishReasonText : $"{failure.Detail} {finishReasonText}";
    }

    private async Task<AiReviewRun> StoreSucceededRun(RunRecord record, AiReviewDraft draft)
    {
        var run = CreateRun(record, AiReviewRunStatus.Succeeded, draft.Summary, failure: null);
        var findings = draft.Findings.Select((finding, index) => CreateFinding(record, finding, index + 1));

        foreach (var finding in findings)
        {
            run.Findings.Add(finding);
        }

        await StoreRun(record, run);

        return run;
    }

    private async Task StoreRun(RunRecord record, AiReviewRun run)
    {
        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.AiReviews.AddAsync(run, CancellationToken.None);
            await UnitOfWork.CompleteAsync(CancellationToken.None);
            await AppendCompletedEvent(record, run);
            await UnitOfWork.CompleteAsync(CancellationToken.None);
        });
    }

    private async Task AppendCompletedEvent(RunRecord record, AiReviewRun run)
    {
        var hasSucceeded = run.Status == AiReviewRunStatus.Succeeded;
        var payload = new AssistantReviewCompletedPayload
        {
            RunId = run.Id,
            Outcome = hasSucceeded ? "succeeded" : "failed",
            ErrorCode = run.ErrorCode,
            FindingCount = run.Findings.Count,
            Model = run.Model,
            ReviewedFingerprint = run.ReviewedFingerprint,
            SubjectUpdatedAt = run.SubjectUpdatedAt,
        };

        var reviewEvent = new EventWriteRequest<AssistantReviewCompletedPayload>
        {
            WorkspaceId = record.WorkspaceId,
            EventKey = EventKeys.AssistantReviewCompleted,
            SubjectType = EventEntityTypes.From(record.Subject.Type),
            SubjectId = record.Subject.Id.ToString(),
            CorrelationId = run.CorrelationId,
            Payload = payload,
        };

        await EventRecords.Append(reviewEvent, CancellationToken.None);
    }

    private static AiReviewRun CreateRun(
        RunRecord record,
        AiReviewRunStatus status,
        string? summary,
        FailureRecord? failure)
    {
        return new AiReviewRun
        {
            WorkspaceId = record.WorkspaceId,
            SubjectType = record.Subject.Type,
            SubjectId = record.Subject.Id,
            Status = status,
            CorrelationId = Guid.NewGuid(),
            ReviewedFingerprint = record.Context.Fingerprint,
            BaselineFingerprint = record.Context.Fingerprint,
            SubjectUpdatedAt = record.Context.UpdatedAt,
            Provider = record.Provider,
            Model = record.Model,
            Summary = summary,
            ErrorCode = failure?.Code,
            FailureDetail = failure?.Detail,
            RawReply = failure?.RawReply,
            StartedAt = record.StartedAt,
            CreatedByUserId = record.UserId,
            OwnerId = record.UserId,
        };
    }

    private static AiReviewFinding CreateFinding(RunRecord record, AiReviewDraftFinding draft, int sequence)
    {
        var finding = new AiReviewFinding
        {
            Sequence = sequence,
            TargetType = record.Subject.Type,
            TargetId = record.Subject.Id,
            Category = draft.Category,
            Title = draft.Title,
            Explanation = draft.Explanation,
            Quote = draft.Quote,
            Suggestion = draft.Suggestion,
            Status = AiReviewFindingStatus.Open,
        };

        var role = draft.Category == AiReviewFindingCategory.Overlap
            ? AiReviewReferenceRole.Overlaps
            : AiReviewReferenceRole.Related;

        foreach (var taskId in draft.RelatedTaskIds)
        {
            finding.References.Add(new AiReviewFindingReference
            {
                Role = role,
                EntityType = EntityType.Task,
                EntityId = taskId,
            });
        }

        return finding;
    }

    private static string BuildPrompt(AiReviewContext context, List<AiReviewDismissal> dismissed)
    {
        var builder = new StringBuilder();

        builder.AppendLine(context.Document);
        builder.AppendLine();
        builder.AppendLine("<dismissed>");

        foreach (var dismissal in dismissed)
        {
            var category = dismissal.Category.ToString().ToLowerInvariant();
            var title = AiReviewPrompt.Neutralise(dismissal.Title);
            var reasonText = dismissal.Reason is null ? null : AiReviewPrompt.Neutralise(dismissal.Reason);
            var reason = reasonText is null ? string.Empty : $" (reason: {reasonText})";

            builder.AppendLine($"- {category}: {title}{reason}");
        }

        builder.Append("</dismissed>");

        return builder.ToString();
    }

    private async Task BookUsage(
        AiUsageOrigin origin,
        AiResolvedCredential credential,
        AiChatRequest request,
        ReviewOutcome outcome,
        bool wasInterrupted)
    {
        // A call stopped or timed out was still sent and paid for, even if the model was thinking and had
        // not written anything yet.
        var wasBilled = outcome.Turn is not null || outcome.Characters > 0 || wasInterrupted;

        if (!wasBilled)
        {
            return;
        }

        var usage = outcome.Turn?.Usage ?? AiUsageEstimate.For(request, outcome.Characters);

        await AiUsageRecorder.Record(UnitOfWork, origin, credential, request.Model, usage, CancellationToken.None);
        await AiHandlerCredentials.MarkUsed(UnitOfWork, credential, CancellationToken.None);
        await UnitOfWork.CompleteAsync(CancellationToken.None);
    }

    private static async IAsyncEnumerable<AiStreamEvent> StreamReply(
        IAiChatProvider provider,
        AiChatRequest request,
        string apiKey,
        ReviewOutcome outcome,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var providerEvent in provider.Stream(request, apiKey, cancellationToken))
        {
            if (providerEvent.CompletedTurn is not null)
            {
                outcome.Turn = providerEvent.CompletedTurn;

                continue;
            }

            if (!string.IsNullOrEmpty(providerEvent.TextDelta))
            {
                outcome.Characters += providerEvent.TextDelta.Length;

                yield return AiStreamEvent.Delta(providerEvent.TextDelta);
            }
        }
    }

    private sealed record RunRecord
    {
        public required AiReviewSubject Subject { get; init; }

        public int WorkspaceId { get; init; }

        public required string UserId { get; init; }

        public required AiReviewContext Context { get; init; }

        public AiProvider Provider { get; init; }

        public required string Model { get; init; }

        public DateTime StartedAt { get; init; }
    }

    private sealed record ReviewFailure(string Code, string? Detail);

    private sealed record FailureRecord
    {
        public required string Code { get; init; }

        public required string Detail { get; init; }

        public string? RawReply { get; init; }
    }

    private sealed class ReviewOutcome
    {
        public AiChatTurn? Turn { get; set; }

        public int Characters { get; set; }

        public string? Failure { get; set; }
    }
}
