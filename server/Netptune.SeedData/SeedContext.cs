using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.SeedData;

public sealed class SeedContext
{
    public List<AppUser> Users { get; } = [];
    public List<Workspace> Workspaces { get; } = [];
    public List<WorkspaceAppUser> WorkspaceUsers { get; } = [];
    public List<Project> Projects { get; } = [];
    public List<ProjectUser> ProjectUsers { get; } = [];
    public List<Sprint> Sprints { get; } = [];
    public List<Board> Boards { get; } = [];
    public List<BoardGroup> BoardGroups { get; } = [];
    public List<ProjectTask> Tasks { get; } = [];
    public List<ProjectTaskAppUser> TaskAssignees { get; } = [];
    public List<ActivityLog> ActivityLogs { get; } = [];
    public List<Comment> Comments { get; } = [];
    public List<Tag> Tags { get; } = [];
    public List<ProjectTaskTag> TaskTags { get; } = [];
    public List<Notification> Notifications { get; } = [];

    public List<AppUser> UsersFor(Workspace workspace) =>
        WorkspaceUsers.Where(wu => wu.Workspace == workspace).Select(wu => wu.User).ToList();
}
