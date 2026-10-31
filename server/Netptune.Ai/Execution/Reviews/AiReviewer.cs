using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Providers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;
using Netptune.Handlers.Ai;

namespace Netptune.Ai.Execution.Reviews;

public sealed class AiReviewer : IAiReviewer
{
    private const int MaximumOutputTokens = 32_000;
    private const int ProgressEveryCharacters = 200;
    private const int MaximumStoredReplyCharacters = 8_000;
    private const int MaximumFailureDetailLength = 1_000;

    // Write tools would propose into a chat change set that a review never shows, and nobody is there to
    // answer a question, so a review reads and its findings are its only way to suggest a change.
    private static readonly IReadOnlySet<AiToolKind> ReviewToolKinds = new HashSet<AiToolKind> { AiToolKind.Read };

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;
    private readonly IAiSpendService Spend;
    private readonly IAiConversationRunner Runner;
    private readonly IEventRecordWriter EventRecords;
    private readonly IReadOnlyDictionary<EntityType, IAiReviewSubject> Subjects;
    private readonly AiOptions Options;
    private readonly ILogger<AiReviewer> Logger;

    public AiReviewer(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector,
        IAiSpendService spend,
        IAiConversationRunner runner,
        IEventRecordWriter eventRecords,
        IEnumerable<IAiReviewSubject> subjects,
        IOptions<AiOptions> options,
        ILogger<AiReviewer> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
        Spend = spend;
        Runner = runner;
        EventRecords = eventRecords;
        Subjects = subjects.ToDictionary(subject => subject.Type);
        Options = options.Value;
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

        var permissions = membership.Permissions.ToHashSet(StringComparer.Ordinal);
        var loadRequest = new AiReviewLoadRequest
        {
            WorkspaceId = workspaceId,
            SubjectId = subject.Id,
            Permissions = permissions,
        };

        var context = await source.Load(loadRequest, cancellationToken);

        if (context is null)
        {
            yield return AiReviewStreamEvent.Failed(source.MissingMessage);

            yield break;
        }

        var dismissed = await UnitOfWork.AiReviews.GetDismissals(subject, workspaceId, cancellationToken);
        var startedAt = DateTime.UtcNow;
        var model = AiModels.ResolveReviewModel(credential.Provider, credential.ReviewModel);
        var prompt = new AiChatMessage { Role = AiMessageRole.User, Text = BuildPrompt(context, dismissed) };

        // The web fetch allowance is deliberately left empty: anyone in the workspace can write the task, so
        // nothing in it may decide which hosts the review can reach. Only web search results are fetchable.
        var runContext = new AiRunContext
        {
            Provider = credential.Provider,
            Model = model,
            ApiKey = Protector.Unprotect(credential.Secret),
            SystemPrompt = AiReviewPrompt.SystemPrompt,
            History = [prompt],
            Permissions = permissions,
            SpendRemaining = spend.Remaining,
            ToolKinds = ReviewToolKinds,
            CorrectsProposalClaims = false,
            MaxToolIterations = Options.ReviewMaxToolIterations,
            MaxTurnCharacters = Options.ReviewMaxTurnCharacters,
            MaxOutputTokens = MaximumOutputTokens,
        };

        var outcome = new ReviewOutcome();

        using var timeout = new CancellationTokenSource(Options.ReviewTimeout);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var run = Runner.Run(runContext, cancellation.Token);
        var events = AiTurnReader.Read(run, Options.ReviewTimeout, timeout.Token, cancellation.Token);
        var origin = new AiUsageOrigin(workspaceId, userId, context.UsagePurpose);
        var reportedCharacters = 0;

        // Only a finally block still runs when the caller stops enumerating, and the tokens were spent either way.
        try
        {
            await foreach (var streamEvent in events)
            {
                var reviewEvent = Observe(streamEvent, runContext, outcome);

                if (reviewEvent is not null)
                {
                    yield return reviewEvent;
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
            await BookUsage(origin, credential, model, runContext.Turns);
        }

        var record = new RunRecord
        {
            Subject = subject,
            WorkspaceId = workspaceId,
            UserId = userId,
            Context = context,
            Provider = credential.Provider,
            Model = model,
            StartedAt = startedAt,
            ToolCalls = DescribeToolCalls(runContext.Invocations),
        };

        var failure = ResolveFailure(outcome, timeout.IsCancellationRequested);

        if (failure is not null)
        {
            var failedRun = await StoreFailedRun(record, failure, outcome);
            var message = DescribeFailure(failure.Code, outcome);

            yield return AiReviewStreamEvent.Failed(message, failedRun.Id);

            yield break;
        }

        var parsed = AiReviewReply.Parse(outcome.Turn!.Text);

        if (parsed.Reply is null)
        {
            var unreadable = new ReviewFailure(AiReviewErrorCodes.UnreadableReply, parsed.Problem);
            var unreadableRun = await StoreFailedRun(record, unreadable, outcome);

            yield return AiReviewStreamEvent.Failed(AppText.AiReviewUnreadable, unreadableRun.Id);

            yield break;
        }

        var reviewContext = await WithToolFoundTasks(source, record, parsed.Reply, cancellationToken);
        var draft = AiReviewValidator.Validate(parsed.Reply, reviewContext, source.Categories);
        var storedRun = await StoreSucceededRun(record, draft);

        yield return AiReviewStreamEvent.Completed(storedRun.Id);
    }

    private static AiReviewStreamEvent? Observe(
        AiStreamEvent streamEvent,
        AiRunContext runContext,
        ReviewOutcome outcome)
    {
        switch (streamEvent.Type)
        {
            case AiStreamEventType.TextDelta:
                outcome.Characters += streamEvent.Text?.Length ?? 0;

                return null;

            case AiStreamEventType.ToolStarted:
                return AiReviewStreamEvent.ToolStarted(streamEvent.ToolName ?? string.Empty);

            case AiStreamEventType.TurnCompleted:
                outcome.Turn = runContext.Turns.LastOrDefault();

                return null;

            case AiStreamEventType.Error:
                outcome.Failure = streamEvent.Message ?? AppText.AiErrorProviderUnreachable;
                outcome.FailureCode = streamEvent.Code;

                return null;

            default:
                return null;
        }
    }

    private static async Task<AiReviewContext> WithToolFoundTasks(
        IAiReviewSubject source,
        RunRecord record,
        AiReviewReply reply,
        CancellationToken cancellationToken)
    {
        var candidates = record.Context.CandidateTasks;
        var unknownKeys = AiReviewValidator.ReferencedTaskKeys(reply)
            .Where(systemId => !candidates.ContainsKey(systemId))
            .ToList();

        if (unknownKeys.Count == 0)
        {
            return record.Context;
        }

        var lookup = new AiReviewTaskLookup(record.WorkspaceId, record.Subject.Id, unknownKeys);
        var found = await source.ResolveTasks(lookup, cancellationToken);
        var merged = new Dictionary<string, int>(candidates, StringComparer.OrdinalIgnoreCase);

        foreach (var (systemId, taskId) in found)
        {
            merged.TryAdd(systemId, taskId);
        }

        return record.Context with { CandidateTasks = merged };
    }

    private static List<AiReviewToolCallViewModel> DescribeToolCalls(List<AiToolInvocationRecord> invocations)
    {
        return invocations
            .Select(invocation => new AiReviewToolCallViewModel
            {
                Name = invocation.ToolName,
                Label = invocation.Label ?? invocation.ToolName,
                IsError = invocation.IsError,
                Truncated = invocation.Truncated,
            })
            .ToList();
    }

    private ReviewFailure? ResolveFailure(ReviewOutcome outcome, bool hasTimedOut)
    {
        if (hasTimedOut)
        {
            var timedOutDetail = $"No completed reply after {Options.ReviewTimeout.TotalMinutes} minutes.";

            return new ReviewFailure(AiReviewErrorCodes.TimedOut, timedOutDetail);
        }

        if (outcome.Failure is not null)
        {
            var failureCode = outcome.FailureCode ?? AiReviewErrorCodes.ProviderError;

            return new ReviewFailure(failureCode, outcome.Failure);
        }

        if (outcome.Turn is null)
        {
            return new ReviewFailure(AiReviewErrorCodes.ProviderError, "The stream ended without a completed turn.");
        }

        var finishReason = outcome.Turn.FinishReason;
        var wasRefused = AiFinishReasons.IsRefusal(finishReason);

        if (wasRefused)
        {
            return new ReviewFailure(AiReviewErrorCodes.Refused, null);
        }

        var isEmpty = string.IsNullOrWhiteSpace(outcome.Turn.Text);

        return isEmpty ? new ReviewFailure(AiReviewErrorCodes.EmptyReply, null) : null;
    }

    private string DescribeFailure(string failureCode, ReviewOutcome outcome)
    {
        var fallback = outcome.Failure ?? AppText.AiReviewUnreadable;

        return failureCode switch
        {
            AiReviewErrorCodes.OutputLimit => AppText.AiReviewHitOutputLimit,
            AiReviewErrorCodes.Refused => AppText.AiReviewRefused,
            AiReviewErrorCodes.EmptyReply => AppText.AiReviewEmptyReply,
            AiReviewErrorCodes.ToolRounds => Localized.Format(
                AppText.AiReviewToolRoundsReached,
                Options.ReviewMaxToolIterations),
            AiReviewErrorCodes.ToolBudget => AppText.AiReviewToolBudgetReached,
            AiReviewErrorCodes.SpendCap => AppText.AiReviewSpendCapReached,
            _ => fallback,
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
            "Assistant review run {RunId} for {SubjectType} {SubjectId} failed with {ErrorCode} using {Provider} {Model} after {Characters} characters and {ToolCalls} tool calls: {FailureDetail}",
            run.Id,
            record.Subject.Type,
            record.Subject.Id,
            failure.Code,
            record.Provider,
            record.Model,
            outcome.Characters,
            record.ToolCalls.Count,
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
            ToolCalls = JsonSerializer.SerializeToDocument(record.ToolCalls, JsonSerializerOptions.Web),
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
        string model,
        List<AiChatTurn> turns)
    {
        // The runner adds an estimate for a call stopped or cut off part way, which was still sent and paid for.
        if (turns.Count == 0)
        {
            return;
        }

        var usage = turns.Aggregate(new AiUsage(), (total, turn) => total.Add(turn.Usage));

        await AiUsageRecorder.Record(UnitOfWork, origin, credential, model, usage, CancellationToken.None);
        await AiHandlerCredentials.MarkUsed(UnitOfWork, credential, CancellationToken.None);
        await UnitOfWork.CompleteAsync(CancellationToken.None);
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

        public required List<AiReviewToolCallViewModel> ToolCalls { get; init; }
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

        public string? FailureCode { get; set; }
    }
}
