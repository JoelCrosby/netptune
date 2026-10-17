using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Webhooks;

internal sealed record ScmTaskActivity
{
    public required string EventKey { get; init; }

    public required IReadOnlyCollection<int> TaskIds { get; init; }

    public required SourceControlRepo Repo { get; init; }

    public required SourceControlProvider Provider { get; init; }

    public required string Reference { get; init; }

    public required string Title { get; init; }

    public required string WebUrl { get; init; }

    public ScmIdentity? Actor { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public int? CommitCount { get; init; }

    public ScmCheckSummaryCounts? Checks { get; init; }
}

internal sealed record ScmCheckSummaryCounts(int Total, int Failed);

internal sealed class ScmTaskActivityRecorder
{
    private const string TaskSubjectType = "task";

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public ScmTaskActivityRecorder(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
    }

    public async Task Record(ScmTaskActivity activity, CancellationToken cancellationToken)
    {
        if (activity.TaskIds.Count == 0)
        {
            return;
        }

        var actorUserId = await ResolveActor(activity, cancellationToken);

        var payload = new SourceControlActivityPayload
        {
            Provider = SourceControlProviderSlugs.From(activity.Provider),
            RepoFullName = activity.Repo.FullName,
            Reference = activity.Reference,
            Title = activity.Title,
            WebUrl = activity.WebUrl,
            CommitCount = activity.CommitCount,
            ChecksTotal = activity.Checks?.Total,
            ChecksFailed = activity.Checks?.Failed,
            ActorLogin = activity.Actor?.Username,
            ActorAvatarUrl = activity.Actor?.AvatarUrl,
        };

        var requests = activity.TaskIds
            .Distinct()
            .Select(taskId => (IEventWriteRequest)new EventWriteRequest<SourceControlActivityPayload>
            {
                WorkspaceId = activity.Repo.WorkspaceId,
                EventKey = activity.EventKey,
                SubjectType = TaskSubjectType,
                SubjectId = taskId.ToString(),
                Payload = payload,
                ActorUserId = actorUserId,
                ResolveActorFromIdentity = false,
                OccurredAt = activity.OccurredAt.UtcDateTime,
            })
            .ToList();

        await EventRecords.AppendRange(requests, cancellationToken);
    }

    private async Task<string?> ResolveActor(ScmTaskActivity activity, CancellationToken cancellationToken)
    {
        var actor = activity.Actor;

        if (actor is null)
        {
            return null;
        }

        List<string> externalIds = actor.ExternalId is null ? [] : [actor.ExternalId];
        List<string> usernames = string.IsNullOrWhiteSpace(actor.Username)
            ? []
            : [UserSourceControlIdentity.Normalize(actor.Username)];
        List<string> emails = actor.Email is null ? [] : [ScmAuthorMatcher.NormalizeEmail(actor.Email)];

        var identities = await UnitOfWork.UserSourceControlIdentities.GetMemberIdentities(
            activity.Repo.WorkspaceId,
            activity.Provider,
            externalIds,
            usernames,
            cancellationToken);

        var membersByEmail = await GetMembersByEmail(activity.Repo.WorkspaceId, emails, cancellationToken);

        var matcher = new ScmAuthorMatcher(identities, membersByEmail);
        var member = matcher.Match(activity.Provider, actor.ExternalId, actor.Username, actor.Email);

        return member?.Id;
    }

    private async Task<List<AppUser>> GetMembersByEmail(
        int workspaceId,
        List<string> emails,
        CancellationToken cancellationToken)
    {
        if (emails.Count == 0)
        {
            return [];
        }

        var members = await UnitOfWork.UserSourceControlIdentities.GetMembersByEmail(
            workspaceId,
            emails,
            cancellationToken);

        return members;
    }
}
