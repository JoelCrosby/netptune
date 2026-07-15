using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories.UnitOfWork;

public class NetptuneUnitOfWork : UnitOfWork<DataContext, IDbConnectionFactory>, INetptuneUnitOfWork
{
    public IProjectRepository Projects { get; }
    public IWorkspaceRepository Workspaces { get; }
    public IWorkspaceUserRepository WorkspaceUsers { get; }
    public ITaskRepository Tasks { get; }
    public ISprintRepository Sprints { get; }
    public IUserRepository Users { get; }
    public IBoardRepository Boards { get; }
    public IBoardGroupRepository BoardGroups { get; }
    public ITaskInGroupRepository ProjectTasksInGroups { get; }
    public IProjectTaskTagRepository ProjectTaskTags { get; }
    public IProjectTaskRelationRepository ProjectTaskRelations { get; }
    public ICommentRepository Comments { get; }
    public IReactionRepository Reactions { get; }
    public ITagRepository Tags { get; }
    public IStatusRepository Statuses { get; }
    public IRelationTypeRepository RelationTypes { get; }
    public IActivityLogRepository ActivityLogs { get; }
    public IActivityEntryRepository ActivityEntries { get; }
    public IAutomationRepository Automations { get; }
    public IFlagRepository Flags { get; }
    public INotificationRepository Notifications { get; }
    public IUserPreferenceRepository UserPreferences { get; }
    public ICommandPaletteRecentItemRepository CommandPaletteRecentItems { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IAncestorRepository Ancestors { get; }
    public IWorkspaceInviteRepository WorkspaceInvites { get; }
    public IWorkspaceFileRepository WorkspaceFiles { get; }
    public ITaskFileRepository TaskFiles { get; }

    public NetptuneUnitOfWork(
        DataContext context,
        IDbConnectionFactory connectionFactory) : base(context, connectionFactory)
    {
        Projects = new ProjectRepository(context, connectionFactory);
        Tasks = new TaskRepository(context, connectionFactory);
        Sprints = new SprintRepository(context, connectionFactory);
        Users = new UserRepository(context, connectionFactory);
        Workspaces = new WorkspaceRepository(context, connectionFactory);
        WorkspaceUsers = new WorkspaceUserRepository(context, connectionFactory);
        Boards = new BoardRepository(context, connectionFactory);
        BoardGroups = new BoardGroupRepository(context, connectionFactory);
        ProjectTasksInGroups = new TaskInGroupRepository(context, connectionFactory);
        ProjectTaskTags = new ProjectTaskTagRepository(context, connectionFactory);
        ProjectTaskRelations = new ProjectTaskRelationRepository(context, connectionFactory);
        Comments = new CommentRepository(context, connectionFactory);
        Reactions = new ReactionRepository(context, connectionFactory);
        Tags = new TagRepository(context, connectionFactory);
        Statuses = new StatusRepository(context, connectionFactory);
        RelationTypes = new RelationTypeRepository(context, connectionFactory);
        ActivityLogs = new ActivityLogRepository(context, connectionFactory);
        ActivityEntries = new ActivityEntryRepository(context, connectionFactory);
        Automations = new AutomationRepository(context, connectionFactory);
        Flags = new FlagRepository(context, connectionFactory);
        Notifications = new NotificationRepository(context, connectionFactory);
        UserPreferences = new UserPreferenceRepository(context, connectionFactory);
        CommandPaletteRecentItems = new CommandPaletteRecentItemRepository(context, connectionFactory);
        RefreshTokens = new RefreshTokenRepository(context, connectionFactory);
        Ancestors = new AncestorRepository(connectionFactory);
        WorkspaceInvites = new WorkspaceInviteRepository(context, connectionFactory);
        WorkspaceFiles = new WorkspaceFileRepository(context, connectionFactory);
        TaskFiles = new TaskFileRepository(context, connectionFactory);
    }
}
