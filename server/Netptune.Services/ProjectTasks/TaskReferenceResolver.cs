using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.ProjectTasks;

public sealed class TaskReferenceResolver : ITaskReferenceResolver
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public TaskReferenceResolver(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<TaskAssigneeResolution> ResolveAssignees(
        IReadOnlyCollection<string>? userIds,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (userIds is null)
        {
            return TaskAssigneeResolution.Unchanged();
        }

        var assigneeIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var containsInvalidAssignee = assigneeIds.Count != userIds.Count;

        if (containsInvalidAssignee)
        {
            return TaskAssigneeResolution.Failed("Assignee IDs cannot be empty or duplicated");
        }

        var clearsEveryAssignee = assigneeIds.Count == 0;

        if (clearsEveryAssignee)
        {
            return new TaskAssigneeResolution { ShouldUpdate = true };
        }

        var assignees = await UnitOfWork.Users.IsUserInWorkspaceRange(assigneeIds, workspaceId, cancellationToken);
        var validAssigneeIds = assignees.Select(assignee => assignee.Id).ToHashSet(StringComparer.Ordinal);
        var missingAssigneeIds = assigneeIds.Where(id => !validAssigneeIds.Contains(id)).ToList();

        if (missingAssigneeIds.Count > 0)
        {
            var error = $"Assignees were not found in the workspace: {string.Join(", ", missingAssigneeIds)}";

            return TaskAssigneeResolution.Failed(error);
        }

        return new TaskAssigneeResolution { ShouldUpdate = true, UserIds = assigneeIds };
    }

    public async Task<TaskTagResolution> ResolveTags(
        IReadOnlyCollection<string>? tagNames,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (tagNames is null)
        {
            return TaskTagResolution.Unchanged();
        }

        var requestedNames = tagNames
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var containsInvalidTag = requestedNames.Count != tagNames.Count;

        if (containsInvalidTag)
        {
            return TaskTagResolution.Failed("Tags cannot be empty or duplicated");
        }

        var clearsEveryTag = requestedNames.Count == 0;

        if (clearsEveryTag)
        {
            return new TaskTagResolution { ShouldUpdate = true };
        }

        var tags = await UnitOfWork.Tags.GetTagsByValueInWorkspace(workspaceId, requestedNames, true, cancellationToken);
        var foundTagNames = tags.Select(tag => tag.Name).ToHashSet(StringComparer.Ordinal);
        var missingTags = requestedNames.Where(tag => !foundTagNames.Contains(tag)).ToList();

        if (missingTags.Count > 0)
        {
            var error = $"Tags were not found in the workspace: {string.Join(", ", missingTags)}";

            return TaskTagResolution.Failed(error);
        }

        return new TaskTagResolution { ShouldUpdate = true, Tags = tags };
    }
}
