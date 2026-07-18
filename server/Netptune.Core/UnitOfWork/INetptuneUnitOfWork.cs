using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.UnitOfWork;

public interface INetptuneUnitOfWork : IUnitOfWork
{
    IProjectRepository Projects { get; }

    IWorkspaceRepository Workspaces { get; }

    IWorkspaceUserRepository WorkspaceUsers { get; }

    ITaskRepository Tasks { get; }

    ISprintRepository Sprints { get; }

    IUserRepository Users { get; }

    IBoardRepository Boards { get; }

    IBoardGroupRepository BoardGroups { get; }

    ITaskInGroupRepository ProjectTasksInGroups { get; }

    IProjectTaskTagRepository ProjectTaskTags { get; }

    IProjectTaskRelationRepository ProjectTaskRelations { get; }

    ICommentRepository Comments { get; }

    IReactionRepository Reactions { get; }

    ITagRepository Tags { get; }

    IStatusRepository Statuses { get; }

    IRelationTypeRepository RelationTypes { get; }

    IActivityLogRepository ActivityLogs { get; }

    IActivityEntryRepository ActivityEntries { get; }

    IAutomationRepository Automations { get; }

    IFlagRepository Flags { get; }

    INotificationRepository Notifications { get; }

    IUserPreferenceRepository UserPreferences { get; }

    ICommandPaletteRecentItemRepository CommandPaletteRecentItems { get; }

    IRefreshTokenRepository RefreshTokens { get; }

    IServiceAccountRepository ServiceAccounts { get; }

    IAncestorRepository Ancestors { get; }

    IWorkspaceInviteRepository WorkspaceInvites { get; }

    IWorkspaceFileRepository WorkspaceFiles { get; }

    ITaskFileRepository TaskFiles { get; }
}
