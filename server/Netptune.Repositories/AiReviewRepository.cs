using System.Text.Json;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Ai;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class AiReviewRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, AiReviewRun, int>(context, connectionFactory), IAiReviewRepository
{
    private const int MentionLimit = 50;
    private const int DismissalLimit = 40;

    private static readonly AiReviewReferenceRole[] MentionRoles =
    [
        AiReviewReferenceRole.Related,
        AiReviewReferenceRole.Overlaps,
    ];

    public Task<List<AiReviewRunViewModel>> GetRuns(
        AiReviewSubject subject,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var runs = ForSubject(subject, workspaceId).OrderByDescending(run => run.CreatedAt);

        return ProjectRuns(runs).ToListAsync(cancellationToken);
    }

    public Task<AiReviewRunViewModel?> GetRun(
        int runId,
        AiReviewSubject subject,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var runs = ForSubject(subject, workspaceId).Where(run => run.Id == runId);

        return ProjectRuns(runs).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AiReviewFindingViewModel>> GetFindings(
        int runId,
        CancellationToken cancellationToken = default)
    {
        var findings = await Context.AiReviewFindings
            .AsNoTracking()
            .Include(finding => finding.References)
            .Where(finding => finding.RunId == runId)
            .OrderBy(finding => finding.Sequence)
            .ToListAsync(cancellationToken);

        var deciderIds = findings
            .Select(finding => finding.DecidedByUserId)
            .OfType<string>()
            .Distinct()
            .ToList();

        var deciderNames = await Context.AppUsers
            .AsNoTracking()
            .Where(user => deciderIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        var referencedTaskIds = findings
            .SelectMany(finding => finding.References)
            .Where(reference => reference.EntityType == EntityType.Task)
            .Select(reference => reference.EntityId)
            .Distinct()
            .ToList();

        var taskLabels = await GetTaskLabels(referencedTaskIds, cancellationToken);

        return findings.Select(finding => ToViewModel(finding, deciderNames, taskLabels)).ToList();
    }

    public async Task<List<AiReviewDismissal>> GetDismissals(
        AiReviewSubject subject,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var dismissals = await Context.AiReviewFindings
            .AsNoTracking()
            .Where(finding =>
                finding.Status == AiReviewFindingStatus.Dismissed &&
                finding.Run.WorkspaceId == workspaceId &&
                finding.Run.SubjectType == subject.Type &&
                finding.Run.SubjectId == subject.Id &&
                finding.Run.Status == AiReviewRunStatus.Succeeded &&
                !finding.Run.IsDeleted)
            .OrderByDescending(finding => finding.DecidedAt)
            .Select(finding => new AiReviewDismissal(finding.Category, finding.Title, finding.DismissReason))
            .Take(DismissalLimit)
            .ToListAsync(cancellationToken);

        return dismissals
            .DistinctBy(dismissal => (dismissal.Category, dismissal.Title.ToLowerInvariant()))
            .ToList();
    }

    public async Task<List<AiReviewMentionViewModel>> GetMentions(
        AiReviewSubject subject,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var candidates = await Context.AiReviewFindingReferences
            .AsNoTracking()
            .Where(reference =>
                reference.EntityType == subject.Type &&
                reference.EntityId == subject.Id &&
                MentionRoles.Contains(reference.Role))
            .Where(reference =>
                reference.Finding.Run.WorkspaceId == workspaceId &&
                reference.Finding.Status == AiReviewFindingStatus.Open &&
                !(reference.Finding.Run.SubjectType == subject.Type && reference.Finding.Run.SubjectId == subject.Id))
            .OrderByDescending(reference => reference.Finding.Run.CreatedAt)
            .Select(reference => new MentionRow
            {
                RunId = reference.Finding.RunId,
                SubjectType = reference.Finding.Run.SubjectType,
                SubjectId = reference.Finding.Run.SubjectId,
                FindingId = reference.FindingId,
                FindingTitle = reference.Finding.Title,
                Category = reference.Finding.Category,
            })
            .Take(MentionLimit)
            .ToListAsync(cancellationToken);

        var currentRunIds = await GetCurrentRunIds(candidates, workspaceId, cancellationToken);
        var currentMentions = candidates.Where(mention => currentRunIds.Contains(mention.RunId)).ToList();

        var subjectTaskIds = currentMentions
            .Where(mention => mention.SubjectType == EntityType.Task)
            .Select(mention => mention.SubjectId)
            .Distinct()
            .ToList();

        var taskLabels = await GetTaskLabels(subjectTaskIds, cancellationToken);

        return currentMentions.Select(mention => ToViewModel(mention, taskLabels)).ToList();
    }

    public async Task<AiReviewOriginViewModel?> GetOrigin(
        AiReviewSubject created,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var origin = await Context.AiReviewFindingReferences
            .AsNoTracking()
            .Where(reference =>
                reference.Role == AiReviewReferenceRole.Created &&
                reference.EntityType == created.Type &&
                reference.EntityId == created.Id &&
                reference.Finding.Run.WorkspaceId == workspaceId)
            .OrderByDescending(reference => reference.Id)
            .Select(reference => new
            {
                reference.Finding.RunId,
                reference.Finding.Title,
                reference.Finding.TargetType,
                reference.Finding.TargetId,
                reference.Finding.DecidedByUserId,
                reference.Finding.DecidedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (origin is null)
        {
            return null;
        }

        var parentIsTask = origin.TargetType == EntityType.Task;
        var parentLabels = parentIsTask ? await GetTaskLabels([origin.TargetId], cancellationToken) : [];
        var parent = parentLabels.GetValueOrDefault(origin.TargetId);
        var acceptedBy = await GetDisplayName(origin.DecidedByUserId, cancellationToken);

        return new AiReviewOriginViewModel
        {
            RunId = origin.RunId,
            FindingTitle = origin.Title,
            ParentTaskId = origin.TargetId,
            ParentSystemId = parent?.SystemId,
            ParentName = parent?.Name,
            AcceptedByDisplayName = acceptedBy,
            AcceptedAt = origin.DecidedAt,
        };
    }

    public Task<AiReviewRun?> GetRunForUpdate(int runId, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(run => run.Id == runId && run.WorkspaceId == workspaceId && !run.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AiReviewFinding?> GetFindingForUpdate(
        int runId,
        long findingId,
        CancellationToken cancellationToken = default)
    {
        return Context.AiReviewFindings
            .Where(finding => finding.Id == findingId && finding.RunId == runId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasNewerSucceededRun(AiReviewRun run, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .AnyAsync(other =>
                other.WorkspaceId == run.WorkspaceId &&
                other.SubjectType == run.SubjectType &&
                other.SubjectId == run.SubjectId &&
                other.Status == AiReviewRunStatus.Succeeded &&
                !other.IsDeleted &&
                other.CreatedAt > run.CreatedAt,
                cancellationToken);
    }

    public async Task<Dictionary<int, string>> GetTaskSpecFingerprints(
        int workspaceId,
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetTaskSpecFingerprints,
            new { workspaceId, taskIds = taskIds.ToArray() },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<FingerprintRow>(command);

        return rows.ToDictionary(row => row.TaskId, row => row.Fingerprint);
    }

    private IQueryable<AiReviewRun> ForSubject(AiReviewSubject subject, int workspaceId)
    {
        return Entities
            .AsNoTracking()
            .Where(run =>
                run.WorkspaceId == workspaceId &&
                run.SubjectType == subject.Type &&
                run.SubjectId == subject.Id &&
                !run.IsDeleted);
    }

    private static IQueryable<AiReviewRunViewModel> ProjectRuns(IQueryable<AiReviewRun> runs)
    {
        return runs.Select(run => new AiReviewRunViewModel
        {
            Id = run.Id,
            SubjectType = run.SubjectType,
            SubjectId = run.SubjectId,
            Status = run.Status,
            Provider = run.Provider,
            Model = run.Model,
            Summary = run.Summary,
            ErrorCode = run.ErrorCode,
            StartedAt = run.StartedAt,
            CreatedAt = run.CreatedAt,
            RunByUserId = run.CreatedByUserId,
            RunByDisplayName = run.CreatedByUser == null ? null : run.CreatedByUser.DisplayName,
            RunByPictureUrl = run.CreatedByUser == null ? null : run.CreatedByUser.PictureUrl,
            OpenCount = run.Findings.Count(finding => finding.Status == AiReviewFindingStatus.Open),
            AcceptedCount = run.Findings.Count(finding => finding.Status == AiReviewFindingStatus.Accepted),
            DismissedCount = run.Findings.Count(finding => finding.Status == AiReviewFindingStatus.Dismissed),
            ToolCalls = ReadToolCalls(run.ToolCalls),
            BaselineFingerprint = run.BaselineFingerprint,
        });
    }

    private async Task<HashSet<int>> GetCurrentRunIds(
        List<MentionRow> mentions,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var subjectIds = mentions.Select(mention => mention.SubjectId).Distinct().ToList();
        var subjectTypes = mentions.Select(mention => mention.SubjectType).Distinct().ToList();

        if (subjectIds.Count == 0)
        {
            return [];
        }

        var currentRunIds = await Entities
            .AsNoTracking()
            .Where(run =>
                run.WorkspaceId == workspaceId &&
                run.Status == AiReviewRunStatus.Succeeded &&
                !run.IsDeleted &&
                subjectIds.Contains(run.SubjectId) &&
                subjectTypes.Contains(run.SubjectType))
            .GroupBy(run => new { run.SubjectType, run.SubjectId })
            .Select(group => group.OrderByDescending(run => run.CreatedAt).Select(run => run.Id).First())
            .ToListAsync(cancellationToken);

        return currentRunIds.ToHashSet();
    }

    private async Task<string?> GetDisplayName(string? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return null;
        }

        return await Context.AppUsers
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<int, TaskLabel>> GetTaskLabels(List<int> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        return await Context.ProjectTasks
            .AsNoTracking()
            .Where(task => taskIds.Contains(task.Id) && !task.IsDeleted)
            .Select(task => new TaskLabel
            {
                TaskId = task.Id,
                SystemId = task.Project == null
                    ? task.ProjectScopeId.ToString()
                    : task.Project.Key + "-" + task.ProjectScopeId.ToString(),
                Name = task.Name,
            })
            .ToDictionaryAsync(label => label.TaskId, cancellationToken);
    }

    private static AiReviewFindingViewModel ToViewModel(
        AiReviewFinding finding,
        Dictionary<string, string> deciderNames,
        Dictionary<int, TaskLabel> taskLabels)
    {
        var decidedByDisplayName = finding.DecidedByUserId is null
            ? null
            : deciderNames.GetValueOrDefault(finding.DecidedByUserId);

        var references = finding.References
            .OrderBy(reference => reference.Id)
            .Select(reference => ToViewModel(reference, taskLabels))
            .ToList();

        return new AiReviewFindingViewModel
        {
            Id = finding.Id,
            Sequence = finding.Sequence,
            TargetType = finding.TargetType,
            TargetId = finding.TargetId,
            Category = finding.Category,
            Title = finding.Title,
            Explanation = finding.Explanation,
            Quote = finding.Quote,
            Suggestion = CloneOrNull(finding.Suggestion),
            Status = finding.Status,
            DecidedByUserId = finding.DecidedByUserId,
            DecidedByDisplayName = decidedByDisplayName,
            DecidedAt = finding.DecidedAt,
            DismissReason = finding.DismissReason,
            AppliedChange = CloneOrNull(finding.AppliedChange),
            References = references,
        };
    }

    private static AiReviewReferenceViewModel ToViewModel(
        AiReviewFindingReference reference,
        Dictionary<int, TaskLabel> taskLabels)
    {
        var isTask = reference.EntityType == EntityType.Task;
        var label = isTask ? taskLabels.GetValueOrDefault(reference.EntityId) : null;

        return new AiReviewReferenceViewModel
        {
            Role = reference.Role,
            EntityType = reference.EntityType,
            EntityId = reference.EntityId,
            SystemId = label?.SystemId,
            Name = label?.Name,
        };
    }

    private static AiReviewMentionViewModel ToViewModel(MentionRow mention, Dictionary<int, TaskLabel> taskLabels)
    {
        var isTask = mention.SubjectType == EntityType.Task;
        var label = isTask ? taskLabels.GetValueOrDefault(mention.SubjectId) : null;

        return new AiReviewMentionViewModel
        {
            RunId = mention.RunId,
            SubjectType = mention.SubjectType,
            SubjectId = mention.SubjectId,
            SubjectSystemId = label?.SystemId,
            SubjectName = label?.Name,
            FindingId = mention.FindingId,
            FindingTitle = mention.FindingTitle,
            Category = mention.Category,
        };
    }

    private static JsonElement? CloneOrNull(JsonDocument? document)
    {
        return document?.RootElement.Clone();
    }

    private static List<AiReviewToolCallViewModel>? ReadToolCalls(JsonDocument? document)
    {
        return document?.Deserialize<List<AiReviewToolCallViewModel>>(JsonSerializerOptions.Web);
    }

    private sealed record MentionRow
    {
        public int RunId { get; init; }

        public EntityType SubjectType { get; init; }

        public int SubjectId { get; init; }

        public long FindingId { get; init; }

        public required string FindingTitle { get; init; }

        public AiReviewFindingCategory Category { get; init; }
    }

    private sealed record TaskLabel
    {
        public int TaskId { get; init; }

        public required string SystemId { get; init; }

        public required string Name { get; init; }
    }

    private sealed record FingerprintRow
    {
        public int TaskId { get; init; }

        public required string Fingerprint { get; init; }
    }
}
