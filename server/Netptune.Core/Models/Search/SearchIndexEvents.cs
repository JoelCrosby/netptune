using Netptune.Core.Services.Activity;

namespace Netptune.Core.Models.Search;

// The entity type is a string on the wire because the job server switches on it, so the names live
// here rather than being spelled out at each dispatch.
public static class SearchIndexEntityTypes
{
    public const string Task = "task";

    public const string Project = "project";

    public const string Sprint = "sprint";
}

public static class SearchIndexEvents
{
    public static Task IndexTasks(this IEventPublisher publisher, IReadOnlyList<int> taskIds, string workspaceSlug)
    {
        return Dispatch(publisher, SearchIndexOperation.Index, SearchIndexEntityTypes.Task, taskIds, workspaceSlug);
    }

    public static Task RemoveTasks(this IEventPublisher publisher, IReadOnlyList<int> taskIds, string workspaceSlug)
    {
        return Dispatch(publisher, SearchIndexOperation.Delete, SearchIndexEntityTypes.Task, taskIds, workspaceSlug);
    }

    public static Task IndexSprints(this IEventPublisher publisher, IReadOnlyList<int> sprintIds, string workspaceSlug)
    {
        return Dispatch(publisher, SearchIndexOperation.Index, SearchIndexEntityTypes.Sprint, sprintIds, workspaceSlug);
    }

    public static Task RemoveSprints(this IEventPublisher publisher, IReadOnlyList<int> sprintIds, string workspaceSlug)
    {
        return Dispatch(publisher, SearchIndexOperation.Delete, SearchIndexEntityTypes.Sprint, sprintIds, workspaceSlug);
    }

    public static Task IndexProjects(this IEventPublisher publisher, IReadOnlyList<int> projectIds, string workspaceSlug)
    {
        return Dispatch(publisher, SearchIndexOperation.Index, SearchIndexEntityTypes.Project, projectIds, workspaceSlug);
    }

    private static Task Dispatch(
        IEventPublisher publisher,
        SearchIndexOperation operation,
        string entityType,
        IReadOnlyList<int> entityIds,
        string workspaceSlug)
    {
        return publisher.Dispatch(new SearchIndexEvent
        {
            Operation = operation,
            EntityType = entityType,
            EntityIds = entityIds,
            WorkspaceSlug = workspaceSlug,
        });
    }
}
