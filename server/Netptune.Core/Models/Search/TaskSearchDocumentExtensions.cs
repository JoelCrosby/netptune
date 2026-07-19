using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Models.Search;

public static class TaskSearchDocumentExtensions
{
    public static TaskSearchDocument ToSearchDocument(this TaskViewModel task, string workspaceSlug)
    {
        return new TaskSearchDocument
        {
            Id = $"task_{task.Id}",
            TaskId = task.Id,
            Title = task.Name,
            Description = task.Description,
            WorkspaceSlug = workspaceSlug,
            Status = task.StatusName,
            Priority = task.Priority?.ToString(),
            AssigneeIds = task.Assignees.Select(assignee => assignee.Id).ToList(),
            ProjectId = task.ProjectId,
            ProjectScopeId = task.ProjectScopeId,
            UpdatedAt = (task.UpdatedAt ?? task.CreatedAt).UtcDateTime,
        };
    }
}
