using System.Globalization;
using System.Text;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Relations;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.Search.Queries;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Reviews;

public sealed class TaskReviewSubject : IAiReviewSubject
{
    public const string DescriptionField = "description";

    private const int MaxDescriptionLength = 20_000;
    private const int MaxComments = 20;
    private const int MaxCommentLength = 1_000;
    private const int MaxSimilarTasks = 8;
    private const int SimilarTaskSearchLimit = 20;
    private const int MaxSimilarDescriptionLength = 400;
    private const int MaxWorkspaceTags = 200;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IMediator Mediator;
    private readonly TaskReviewSuggestions Suggestions;

    public TaskReviewSubject(INetptuneUnitOfWork unitOfWork, IMediator mediator, TaskReviewSuggestions suggestions)
    {
        UnitOfWork = unitOfWork;
        Mediator = mediator;
        Suggestions = suggestions;
    }

    public EntityType Type => EntityType.Task;

    public IReadOnlyList<string> RunPermissions { get; } =
    [
        NetptunePermissions.Assistant.Use,
        NetptunePermissions.Tasks.Read,
    ];

    public IReadOnlyList<string> DecidePermissions { get; } =
    [
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Tasks.Update,
    ];

    public IReadOnlySet<AiReviewFindingCategory> Categories { get; } =
        Enum.GetValues<AiReviewFindingCategory>().ToHashSet();

    public string MissingMessage => AppText.AiErrorTaskNotFound;

    public async Task<AiReviewContext?> Load(AiReviewLoadRequest request, CancellationToken cancellationToken)
    {
        var fingerprint = await GetFingerprint(request.WorkspaceId, request.SubjectId, cancellationToken);

        if (fingerprint is null)
        {
            return null;
        }

        var task = await UnitOfWork.Tasks.GetTaskViewModel(request.SubjectId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var relations = await UnitOfWork.ProjectTaskRelations.GetRelationsForTask(
            task.Id,
            request.WorkspaceId,
            cancellationToken);
        var comments = await UnitOfWork.Comments.GetCommentViewModelsForTask(task.Id, cancellationToken);
        var similarTasks = await FindSimilarTasks(request.WorkspaceId, task, relations, cancellationToken);
        var workspaceTags = await UnitOfWork.Tags.GetTagsInWorkspace(request.WorkspaceId, true, cancellationToken);
        var tagNames = workspaceTags.Select(tag => tag.Name).Take(MaxWorkspaceTags).ToList();
        var recentComments = comments.OrderBy(comment => comment.CreatedAt).TakeLast(MaxComments).ToList();
        var candidateTasks = similarTasks.ToDictionary(
            similar => similar.SystemId,
            similar => similar.Id,
            StringComparer.OrdinalIgnoreCase);

        return new AiReviewContext
        {
            Document = BuildDocument(task, relations, recentComments, similarTasks, tagNames),
            Fingerprint = fingerprint,
            UpdatedAt = task.UpdatedAt?.UtcDateTime,
            UsagePurpose = AiUsagePurposes.TaskReview,
            EditableFields = new Dictionary<string, string> { [DescriptionField] = task.Description ?? string.Empty },
            CandidateTasks = candidateTasks,
            WorkspaceTags = tagNames,
        };
    }

    public IReadOnlyList<string> PermissionsFor(AiReviewApplyRequest request)
    {
        return TaskReviewSuggestions.PermissionsFor(request);
    }

    public async Task<AiReviewApplication> ApplySuggestion(
        AiReviewApplyRequest request,
        CancellationToken cancellationToken)
    {
        var task = await GetLiveTask(request.WorkspaceId, request.TargetId, cancellationToken);

        if (task is null)
        {
            return AiReviewApplication.Failed(AppText.AiErrorTaskNotFound);
        }

        return await Suggestions.Apply(request, task, cancellationToken);
    }

    public async Task<string?> GetFingerprint(int workspaceId, int subjectId, CancellationToken cancellationToken)
    {
        var fingerprints = await UnitOfWork.AiReviews.GetTaskSpecFingerprints(
            workspaceId,
            [subjectId],
            cancellationToken);

        return fingerprints.GetValueOrDefault(subjectId);
    }

    public async Task<string?> ReadField(
        int workspaceId,
        int targetId,
        string field,
        CancellationToken cancellationToken)
    {
        var isDescription = field == DescriptionField;

        if (!isDescription)
        {
            return null;
        }

        var task = await GetLiveTask(workspaceId, targetId, cancellationToken);

        return task is null ? null : task.Description ?? string.Empty;
    }

    public async Task<ClientResponse> WriteField(
        int targetId,
        string field,
        string value,
        CancellationToken cancellationToken)
    {
        var isDescription = field == DescriptionField;

        if (!isDescription)
        {
            return ClientResponse.Failed(AppText.AiReviewFieldNotEditable);
        }

        var update = new UpdateProjectTaskRequest { Id = targetId, Description = value };
        var result = await Mediator.Send(new UpdateTaskCommand(update), cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResponse.Failed(result.Message);
        }

        return ClientResponse.Success;
    }

    // The fingerprint query is scoped to the workspace and skips deleted tasks, which the view-model read does not.
    private async Task<TaskViewModel?> GetLiveTask(int workspaceId, int taskId, CancellationToken cancellationToken)
    {
        var fingerprint = await GetFingerprint(workspaceId, taskId, cancellationToken);

        if (fingerprint is null)
        {
            return null;
        }

        return await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);
    }

    private async Task<List<TaskViewModel>> FindSimilarTasks(
        int workspaceId,
        TaskViewModel task,
        List<TaskRelationViewModel> relations,
        CancellationToken cancellationToken)
    {
        var searchQuery = new SearchTaskIdsQuery(task.Name, SimilarTaskSearchLimit);
        var searchResult = await Mediator.Send(searchQuery, cancellationToken);
        var relatedTaskIds = relations.Select(relation => relation.RelatedTask.Id).ToHashSet();

        var candidateIds = (searchResult?.TaskIds ?? [])
            .Where(id => id != task.Id && !relatedTaskIds.Contains(id))
            .Take(MaxSimilarTasks)
            .ToList();

        if (candidateIds.Count == 0)
        {
            return [];
        }

        // The search index drops a deleted task a little after the database does, so its hits are re-read
        // as live tasks of this workspace before they are offered as related.
        var candidates = await UnitOfWork.Tasks.GetLiveTaskViewModels(candidateIds, workspaceId, cancellationToken);
        var rankById = candidateIds.Select((id, rank) => (id, rank)).ToDictionary(pair => pair.id, pair => pair.rank);

        return candidates.OrderBy(candidate => rankById[candidate.Id]).ToList();
    }

    private static string BuildDocument(
        TaskViewModel task,
        List<TaskRelationViewModel> relations,
        List<CommentViewModel> comments,
        List<TaskViewModel> similarTasks,
        List<string> workspaceTags)
    {
        var builder = new StringBuilder();
        var tags = task.Tags.Count > 0 ? string.Join(", ", task.Tags) : "none";
        var assigneeNames = task.Assignees.Select(assignee => AiReviewPrompt.Neutralise(assignee.DisplayName)).ToList();
        var assignees = assigneeNames.Count > 0 ? string.Join(", ", assigneeNames) : "none";
        var estimateValue = task.EstimateValue?.ToString(CultureInfo.InvariantCulture);
        var estimate = estimateValue is null ? "none" : $"{estimateValue} ({task.EstimateType})";

        builder.AppendLine("<task>");
        builder.AppendLine($"Key: {task.SystemId}");
        builder.AppendLine($"Name: {AiReviewPrompt.Neutralise(task.Name)}");
        builder.AppendLine($"Status: {AiReviewPrompt.Neutralise(task.StatusName)}");
        builder.AppendLine($"Project: {AiReviewPrompt.Neutralise(task.ProjectName ?? "none")}");
        builder.AppendLine($"Sprint: {AiReviewPrompt.Neutralise(task.SprintName ?? "none")}");
        builder.AppendLine($"Tags: {AiReviewPrompt.Neutralise(tags)}");
        builder.AppendLine($"Assignees: {assignees}");
        builder.AppendLine($"Estimate: {estimate}");
        builder.AppendLine($"Priority: {task.Priority?.ToString() ?? "none"}");
        builder.AppendLine($"Start date: {task.StartDate?.ToString("yyyy-MM-dd") ?? "none"}");
        builder.AppendLine($"Due date: {task.DueDate?.ToString("yyyy-MM-dd") ?? "none"}");
        builder.AppendLine("</task>");
        builder.AppendLine();

        var description = AiReviewPrompt.Truncate(task.Description ?? string.Empty, MaxDescriptionLength);

        builder.AppendLine($"<description field=\"{DescriptionField}\">");
        builder.AppendLine(AiReviewPrompt.Neutralise(description));
        builder.AppendLine("</description>");
        builder.AppendLine();

        builder.AppendLine("<relations>");

        foreach (var relation in relations)
        {
            var related = relation.RelatedTask;
            var relatedName = AiReviewPrompt.Neutralise(related.Name);
            var label = AiReviewPrompt.Neutralise(relation.Label);
            var relatedStatus = AiReviewPrompt.Neutralise(related.StatusName);

            builder.AppendLine($"- {label} {related.SystemId} \"{relatedName}\" ({relatedStatus})");
        }

        builder.AppendLine("</relations>");
        builder.AppendLine();

        builder.AppendLine("<comments>");

        foreach (var comment in comments)
        {
            var body = AiReviewPrompt.Truncate(comment.Body, MaxCommentLength);
            var commentBody = AiReviewPrompt.Neutralise(body);
            var author = AiReviewPrompt.Neutralise(comment.UserDisplayName);

            builder.AppendLine($"- {author} on {comment.CreatedAt:yyyy-MM-dd}: {commentBody}");
        }

        builder.AppendLine("</comments>");
        builder.AppendLine();

        builder.AppendLine("<similar-tasks>");

        foreach (var similar in similarTasks)
        {
            var summary = AiReviewPrompt.Truncate(similar.Description ?? string.Empty, MaxSimilarDescriptionLength);
            var similarName = AiReviewPrompt.Neutralise(similar.Name);
            var similarSummary = AiReviewPrompt.Neutralise(summary);
            var similarStatus = AiReviewPrompt.Neutralise(similar.StatusName);

            builder.AppendLine($"- {similar.SystemId} \"{similarName}\" ({similarStatus}): {similarSummary}");
        }

        builder.AppendLine("</similar-tasks>");
        builder.AppendLine();
        builder.AppendLine("<workspace-tags>");
        builder.AppendLine(AiReviewPrompt.Neutralise(string.Join(", ", workspaceTags)));
        builder.Append("</workspace-tags>");

        return builder.ToString();
    }
}
