using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.Transfer.Repositories;

public interface IArchiveRepository
{
    Task<Workspace?> GetWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<WorkspaceAppUser> ReadMembers(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Status> ReadStatuses(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Tag> ReadTags(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<RelationType> ReadRelationTypes(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Project> ReadProjects(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Board> ReadBoards(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<BoardGroup> ReadBoardGroups(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Sprint> ReadSprints(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ProjectTask> ReadTasks(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ProjectTaskAppUser> ReadTaskAssignees(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ProjectTaskTag> ReadTaskTags(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ProjectTaskInBoardGroup> ReadTaskPlacements(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Reaction> ReadReactions(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ProjectTaskRelation> ReadTaskRelations(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Comment> ReadComments(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Flag> ReadFlags(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AutomationRule> ReadAutomations(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<WorkspaceFile> ReadFiles(int workspaceId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<EventRecord> ReadEvents(int workspaceId, CancellationToken cancellationToken = default);

    Task<long> GetFileBytes(int workspaceId, CancellationToken cancellationToken = default);
}
