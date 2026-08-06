using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.Transfer.Catalog;

public static class ArchiveCatalog
{
    public static ArchiveRecordDefinition<Workspace> Workspace { get; } = new()
    {
        Key = TransferRecordTypes.Workspace,
        Name = "Workspace",
        FileName = "data/workspace.ndjson",
        Ref = workspace => EntityRefBuilder.ForWorkspace(workspace),
        Bindings =
        [
            ArchiveField.Text<Workspace>(TransferRecordTypes.Workspace, "slug", "Slug", workspace => workspace.Slug),
            ArchiveField.Text<Workspace>(TransferRecordTypes.Workspace, "name", "Name", workspace => workspace.Name),
            ArchiveField.Text<Workspace>(TransferRecordTypes.Workspace, "description", "Description", workspace => workspace.Description, TransferValueType.LongText),
            ArchiveField.Text<Workspace>(TransferRecordTypes.Workspace, "is_public", "Public", workspace => workspace.IsPublic, TransferValueType.Boolean),
            ArchiveField.Text<Workspace>(TransferRecordTypes.Workspace, "created_at", "Created at", workspace => workspace.CreatedAt, TransferValueType.DateTime),
        ],
    };

    public static ArchiveRecordDefinition<WorkspaceAppUser> Member { get; } = new()
    {
        Key = TransferRecordTypes.Member,
        Name = "Member",
        FileName = "data/members.ndjson",
        Ref = member => EntityRefBuilder.ForUser(MemberEmail(member)),
        Bindings =
        [
            ArchiveField.Reference<WorkspaceAppUser>(TransferRecordTypes.Member, "user", "User", EntityRefTypes.User, member => EntityRefBuilder.ForUser(MemberEmail(member))),
            ArchiveField.Text<WorkspaceAppUser>(TransferRecordTypes.Member, "email", "Email", MemberEmail),
            ArchiveField.Text<WorkspaceAppUser>(TransferRecordTypes.Member, "display_name", "Display name", member => member.User.DisplayName),
            ArchiveField.Text<WorkspaceAppUser>(TransferRecordTypes.Member, "role", "Role", member => member.Role, TransferValueType.Enum),
            ArchiveField.Text<WorkspaceAppUser>(TransferRecordTypes.Member, "permissions", "Permissions", member => member.Permissions),
        ],
    };

    public static ArchiveRecordDefinition<Status> Status { get; } = new()
    {
        Key = TransferRecordTypes.Status,
        Name = "Status",
        FileName = "data/statuses.ndjson",
        Ref = status => EntityRefBuilder.ForStatus(status),
        Bindings =
        [
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "key", "Key", status => status.Key),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "name", "Name", status => status.Name),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "description", "Description", status => status.Description, TransferValueType.LongText),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "color", "Colour", status => status.Color),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "sort_order", "Sort order", status => status.SortOrder, TransferValueType.Decimal),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "category", "Category", status => status.Category, TransferValueType.Enum),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "entity_type", "Entity type", status => status.EntityType, TransferValueType.Enum),
            ArchiveField.Text<Status>(TransferRecordTypes.Status, "is_system", "System", status => status.IsSystem, TransferValueType.Boolean),
        ],
    };

    public static ArchiveRecordDefinition<Tag> Tag { get; } = new()
    {
        Key = TransferRecordTypes.Tag,
        Name = "Tag",
        FileName = "data/tags.ndjson",
        Ref = tag => EntityRefBuilder.ForTag(tag),
        Bindings =
        [
            ArchiveField.Text<Tag>(TransferRecordTypes.Tag, "name", "Name", tag => tag.Name),
        ],
    };

    public static ArchiveRecordDefinition<RelationType> RelationType { get; } = new()
    {
        Key = TransferRecordTypes.RelationType,
        Name = "Relation type",
        FileName = "data/relation-types.ndjson",
        Ref = relationType => EntityRefBuilder.ForRelationType(relationType),
        Bindings =
        [
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "key", "Key", relationType => relationType.Key),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "name", "Name", relationType => relationType.Name),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "inverse_name", "Inverse name", relationType => relationType.InverseName),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "description", "Description", relationType => relationType.Description, TransferValueType.LongText),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "color", "Colour", relationType => relationType.Color),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "sort_order", "Sort order", relationType => relationType.SortOrder, TransferValueType.Decimal),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "category", "Category", relationType => relationType.Category, TransferValueType.Enum),
            ArchiveField.Text<RelationType>(TransferRecordTypes.RelationType, "is_system", "System", relationType => relationType.IsSystem, TransferValueType.Boolean),
        ],
    };

    public static ArchiveRecordDefinition<Project> Project { get; } = new()
    {
        Key = TransferRecordTypes.Project,
        Name = "Project",
        FileName = "data/projects.ndjson",
        Ref = project => EntityRefBuilder.ForProject(project),
        Bindings =
        [
            ArchiveField.Text<Project>(TransferRecordTypes.Project, "key", "Key", project => project.Key),
            ArchiveField.Text<Project>(TransferRecordTypes.Project, "name", "Name", project => project.Name),
            ArchiveField.Text<Project>(TransferRecordTypes.Project, "description", "Description", project => project.Description, TransferValueType.LongText),
            ArchiveField.Text<Project>(TransferRecordTypes.Project, "repository_url", "Repository", project => project.RepositoryUrl),
            ArchiveField.Text<Project>(TransferRecordTypes.Project, "color", "Colour", project => project.MetaInfo?.Color),
            ArchiveField.Reference<Project>(TransferRecordTypes.Project, "default_status", "Default status", EntityRefTypes.Status, project => project.DefaultStatus is null ? null : EntityRefBuilder.ForStatus(project.DefaultStatus)),
            ArchiveField.Reference<Project>(TransferRecordTypes.Project, "members", "Members", EntityRefTypes.User, project => project.ProjectUsers.Select(link => EntityRefBuilder.ForUser(UserEmail(link.User))).ToList(), true),
        ],
    };

    public static ArchiveRecordDefinition<Board> Board { get; } = new()
    {
        Key = TransferRecordTypes.Board,
        Name = "Board",
        FileName = "data/boards.ndjson",
        Ref = board => EntityRefBuilder.ForBoard(board),
        Bindings =
        [
            ArchiveField.Text<Board>(TransferRecordTypes.Board, "identifier", "Identifier", board => board.Identifier),
            ArchiveField.Text<Board>(TransferRecordTypes.Board, "name", "Name", board => board.Name),
            ArchiveField.Text<Board>(TransferRecordTypes.Board, "board_type", "Type", board => board.BoardType, TransferValueType.Enum),
            ArchiveField.Text<Board>(TransferRecordTypes.Board, "color", "Colour", board => board.MetaInfo?.Color),
            ArchiveField.Reference<Board>(TransferRecordTypes.Board, "project", "Project", EntityRefTypes.Project, board => board.Project is null ? null : EntityRefBuilder.ForProject(board.Project)),
        ],
    };

    public static ArchiveRecordDefinition<BoardGroup> BoardGroup { get; } = new()
    {
        Key = TransferRecordTypes.BoardGroup,
        Name = "Board group",
        FileName = "data/board-groups.ndjson",
        Ref = group => EntityRefBuilder.ForBoardGroup(BoardIdentifier(group), group.Name),
        Bindings =
        [
            ArchiveField.Text<BoardGroup>(TransferRecordTypes.BoardGroup, "name", "Name", group => group.Name),
            ArchiveField.Text<BoardGroup>(TransferRecordTypes.BoardGroup, "sort_order", "Sort order", group => group.SortOrder, TransferValueType.Decimal),
            ArchiveField.Reference<BoardGroup>(TransferRecordTypes.BoardGroup, "board", "Board", EntityRefTypes.Board, group => group.Board is null ? null : EntityRefBuilder.ForBoard(group.Board)),
            ArchiveField.Reference<BoardGroup>(TransferRecordTypes.BoardGroup, "status", "Status", EntityRefTypes.Status, group => group.Status is null ? null : EntityRefBuilder.ForStatus(group.Status)),
        ],
    };

    public static ArchiveRecordDefinition<Sprint> Sprint { get; } = new()
    {
        Key = TransferRecordTypes.Sprint,
        Name = "Sprint",
        FileName = "data/sprints.ndjson",
        Ref = sprint => EntityRefBuilder.ForSprint(ProjectKey(sprint.Project), sprint.Name),
        Bindings =
        [
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "name", "Name", sprint => sprint.Name),
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "goal", "Goal", sprint => sprint.Goal, TransferValueType.LongText),
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "status", "Status", sprint => sprint.Status, TransferValueType.Enum),
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "start_date", "Start date", sprint => sprint.StartDate, TransferValueType.DateTime),
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "end_date", "End date", sprint => sprint.EndDate, TransferValueType.DateTime),
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "started_at", "Started at", sprint => sprint.StartedAt, TransferValueType.DateTime),
            ArchiveField.Text<Sprint>(TransferRecordTypes.Sprint, "completed_at", "Completed at", sprint => sprint.CompletedAt, TransferValueType.DateTime),
            ArchiveField.Reference<Sprint>(TransferRecordTypes.Sprint, "project", "Project", EntityRefTypes.Project, sprint => sprint.Project is null ? null : EntityRefBuilder.ForProject(sprint.Project)),
        ],
    };

    public static ArchiveRecordDefinition<ProjectTask> Task { get; } = new()
    {
        Key = TransferRecordTypes.Task,
        Name = "Task",
        FileName = "data/tasks.ndjson",
        Ref = task => TaskRef(task) ?? new EntityRef(TransferRecordTypes.Task, EntityRefBuilder.UnnamedSegment),
        Bindings =
        [
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "system_id", "System id", task => TaskRef(task)?.Value),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "scope_id", "Number", task => task.ProjectScopeId, TransferValueType.Integer),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "name", "Name", task => task.Name),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "description", "Description", task => task.Description, TransferValueType.LongText),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "priority", "Priority", task => task.Priority, TransferValueType.Enum),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "estimate_type", "Estimate type", task => task.EstimateType, TransferValueType.Enum),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "estimate_value", "Estimate", task => task.EstimateValue, TransferValueType.Decimal),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "start_date", "Start date", task => task.StartDate, TransferValueType.Date),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "due_date", "Due date", task => task.DueDate, TransferValueType.Date),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "external_id", "External id", task => task.ExternalId),
            ArchiveField.Text<ProjectTask>(TransferRecordTypes.Task, "created_at", "Created at", task => task.CreatedAt, TransferValueType.DateTime),
            ArchiveField.Reference<ProjectTask>(TransferRecordTypes.Task, "status", "Status", EntityRefTypes.Status, task => task.Status is null ? null : EntityRefBuilder.ForStatus(task.Status)),
            ArchiveField.Reference<ProjectTask>(TransferRecordTypes.Task, "project", "Project", EntityRefTypes.Project, task => task.Project is null ? null : EntityRefBuilder.ForProject(task.Project)),
            ArchiveField.Reference<ProjectTask>(TransferRecordTypes.Task, "sprint", "Sprint", EntityRefTypes.Sprint, task => SprintRef(task.Sprint)),
            ArchiveField.Reference<ProjectTask>(TransferRecordTypes.Task, "created_by", "Created by", EntityRefTypes.User, task => AuthorRef(task.CreatedByUser)),
        ],
    };

    public static ArchiveRecordDefinition<ProjectTaskAppUser> TaskAssignee { get; } = new()
    {
        Key = TransferRecordTypes.TaskAssignee,
        Name = "Task assignee",
        FileName = "data/task-assignees.ndjson",
        Ref = link => new EntityRef(TransferRecordTypes.TaskAssignee, $"{TaskRefValue(link.ProjectTask)}#{UserEmail(link.User)}"),
        Bindings =
        [
            ArchiveField.Reference<ProjectTaskAppUser>(TransferRecordTypes.TaskAssignee, "task", "Task", EntityRefTypes.Task, link => TaskRef(link.ProjectTask)),
            ArchiveField.Reference<ProjectTaskAppUser>(TransferRecordTypes.TaskAssignee, "user", "User", EntityRefTypes.User, link => EntityRefBuilder.ForUser(UserEmail(link.User))),
        ],
    };

    public static ArchiveRecordDefinition<ProjectTaskTag> TaskTag { get; } = new()
    {
        Key = TransferRecordTypes.TaskTag,
        Name = "Task tag",
        FileName = "data/task-tags.ndjson",
        Ref = link => new EntityRef(TransferRecordTypes.TaskTag, $"{TaskRefValue(link.ProjectTask)}#{link.Tag?.Name.ToLowerInvariant()}"),
        Bindings =
        [
            ArchiveField.Reference<ProjectTaskTag>(TransferRecordTypes.TaskTag, "task", "Task", EntityRefTypes.Task, link => TaskRef(link.ProjectTask)),
            ArchiveField.Reference<ProjectTaskTag>(TransferRecordTypes.TaskTag, "tag", "Tag", EntityRefTypes.Tag, link => link.Tag is null ? null : EntityRefBuilder.ForTag(link.Tag)),
        ],
    };

    public static ArchiveRecordDefinition<ProjectTaskInBoardGroup> TaskPlacement { get; } = new()
    {
        Key = TransferRecordTypes.TaskPlacement,
        Name = "Task placement",
        FileName = "data/task-placements.ndjson",
        Ref = link => new EntityRef(TransferRecordTypes.TaskPlacement, $"{TaskRefValue(link.ProjectTask)}#{link.BoardGroupId}"),
        Bindings =
        [
            ArchiveField.Reference<ProjectTaskInBoardGroup>(TransferRecordTypes.TaskPlacement, "task", "Task", EntityRefTypes.Task, link => TaskRef(link.ProjectTask)),
            ArchiveField.Reference<ProjectTaskInBoardGroup>(TransferRecordTypes.TaskPlacement, "board_group", "Board group", EntityRefTypes.BoardGroup, link => BoardGroupRef(link.BoardGroup)),
            ArchiveField.Text<ProjectTaskInBoardGroup>(TransferRecordTypes.TaskPlacement, "sort_order", "Sort order", link => link.SortOrder, TransferValueType.Decimal),
        ],
    };

    public static ArchiveRecordDefinition<Reaction> Reaction { get; } = new()
    {
        Key = TransferRecordTypes.Reaction,
        Name = "Reaction",
        FileName = "data/reactions.ndjson",
        Ref = reaction => new EntityRef(TransferRecordTypes.Reaction, $"{reaction.CommentId}#{reaction.Id}"),
        Bindings =
        [
            ArchiveField.Text<Reaction>(TransferRecordTypes.Reaction, "value", "Value", reaction => reaction.Value),
            ArchiveField.Text<Reaction>(TransferRecordTypes.Reaction, "comment_id", "Comment id", reaction => reaction.CommentId, TransferValueType.Integer),
            ArchiveField.Reference<Reaction>(TransferRecordTypes.Reaction, "author", "Author", EntityRefTypes.User, reaction => AuthorRef(reaction.CreatedByUser)),
        ],
    };

    public static ArchiveRecordDefinition<ProjectTaskRelation> TaskRelation { get; } = new()
    {
        Key = TransferRecordTypes.TaskRelation,
        Name = "Task relation",
        FileName = "data/task-relations.ndjson",
        Ref = relation => new EntityRef(TransferRecordTypes.TaskRelation, $"{TaskRefValue(relation.SourceTask)}>{TaskRefValue(relation.TargetTask)}"),
        Bindings =
        [
            ArchiveField.Reference<ProjectTaskRelation>(TransferRecordTypes.TaskRelation, "source", "Source", EntityRefTypes.Task, relation => TaskRef(relation.SourceTask)),
            ArchiveField.Reference<ProjectTaskRelation>(TransferRecordTypes.TaskRelation, "target", "Target", EntityRefTypes.Task, relation => TaskRef(relation.TargetTask)),
            ArchiveField.Reference<ProjectTaskRelation>(TransferRecordTypes.TaskRelation, "relation_type", "Relation type", EntityRefTypes.RelationType, relation => relation.RelationType is null ? null : EntityRefBuilder.ForRelationType(relation.RelationType)),
        ],
    };

    public static ArchiveRecordDefinition<Comment> Comment { get; } = new()
    {
        Key = TransferRecordTypes.Comment,
        Name = "Comment",
        FileName = "data/comments.ndjson",
        Ref = comment => new EntityRef(TransferRecordTypes.Comment, $"{comment.EntityType}-{comment.EntityId}#{comment.Id}"),
        Bindings =
        [
            ArchiveField.Text<Comment>(TransferRecordTypes.Comment, "body", "Body", comment => comment.Body, TransferValueType.LongText),
            ArchiveField.Text<Comment>(TransferRecordTypes.Comment, "entity_type", "Entity type", comment => comment.EntityType, TransferValueType.Enum),
            ArchiveField.Text<Comment>(TransferRecordTypes.Comment, "entity_id", "Entity id", comment => comment.EntityId, TransferValueType.Integer),
            ArchiveField.Reference<Comment>(TransferRecordTypes.Comment, "author", "Author", EntityRefTypes.User, comment => AuthorRef(comment.CreatedByUser)),
            ArchiveField.Text<Comment>(TransferRecordTypes.Comment, "created_at", "Created at", comment => comment.CreatedAt, TransferValueType.DateTime),
        ],
    };

    public static ArchiveRecordDefinition<Flag> Flag { get; } = new()
    {
        Key = TransferRecordTypes.Flag,
        Name = "Flag",
        FileName = "data/flags.ndjson",
        Ref = flag => new EntityRef(TransferRecordTypes.Flag, $"{flag.EntityType}-{flag.EntityId}#{flag.Id}"),
        Bindings =
        [
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "name", "Name", flag => flag.Name),
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "description", "Description", flag => flag.Description, TransferValueType.LongText),
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "entity_type", "Entity type", flag => flag.EntityType, TransferValueType.Enum),
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "entity_id", "Entity id", flag => flag.EntityId, TransferValueType.Integer),
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "resolution", "Resolution", flag => flag.Resolution, TransferValueType.Enum),
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "resolved_at", "Resolved at", flag => flag.ResolvedAt, TransferValueType.DateTime),
            ArchiveField.Text<Flag>(TransferRecordTypes.Flag, "created_at", "Created at", flag => flag.CreatedAt, TransferValueType.DateTime),
        ],
    };

    public static ArchiveRecordDefinition<AutomationRule> Automation { get; } = new()
    {
        Key = TransferRecordTypes.Automation,
        Name = "Automation",
        FileName = "data/automations.ndjson",
        Ref = rule => EntityRefBuilder.ForAutomation(rule),
        Bindings =
        [
            ArchiveField.Text<AutomationRule>(TransferRecordTypes.Automation, "name", "Name", rule => rule.Name),
            ArchiveField.Text<AutomationRule>(TransferRecordTypes.Automation, "is_enabled", "Enabled", rule => rule.IsEnabled, TransferValueType.Boolean),
            ArchiveField.Text<AutomationRule>(TransferRecordTypes.Automation, "trigger_type", "Trigger", rule => rule.TriggerType, TransferValueType.Enum),
            ArchiveField.Text<AutomationRule>(TransferRecordTypes.Automation, "trigger_config", "Trigger configuration", rule => rule.TriggerConfig, TransferValueType.Json),
            ArchiveField.Text<AutomationRule>(TransferRecordTypes.Automation, "actions", "Actions", rule => rule.Actions, TransferValueType.Json),
        ],
    };

    public static ArchiveRecordDefinition<WorkspaceFile> WorkspaceFile { get; } = new()
    {
        Key = TransferRecordTypes.WorkspaceFile,
        Name = "File",
        FileName = "data/workspace-files.ndjson",
        Ref = file => EntityRefBuilder.ForWorkspaceFile(file),
        Bindings =
        [
            ArchiveField.Text<WorkspaceFile>(TransferRecordTypes.WorkspaceFile, "content_id", "Content id", file => file.ContentId),
            ArchiveField.Text<WorkspaceFile>(TransferRecordTypes.WorkspaceFile, "original_name", "Name", file => file.OriginalName),
            ArchiveField.Text<WorkspaceFile>(TransferRecordTypes.WorkspaceFile, "content_type", "Content type", file => file.ContentType),
            ArchiveField.Text<WorkspaceFile>(TransferRecordTypes.WorkspaceFile, "size_bytes", "Size", file => file.SizeBytes, TransferValueType.Integer),
            ArchiveField.Text<WorkspaceFile>(TransferRecordTypes.WorkspaceFile, "purpose", "Purpose", file => file.Purpose, TransferValueType.Enum),
            ArchiveField.Text<WorkspaceFile>(TransferRecordTypes.WorkspaceFile, "created_at", "Created at", file => file.CreatedAt, TransferValueType.DateTime),
        ],
    };

    public static ArchiveRecordDefinition<EventRecord> Event { get; } = new()
    {
        Key = TransferRecordTypes.Event,
        Name = "Event",
        FileName = "data/events.ndjson",
        Ref = record => new EntityRef(TransferRecordTypes.Event, record.EventId.ToString()),
        Bindings =
        [
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "event_id", "Event id", record => record.EventId.ToString()),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "event_key", "Key", record => record.EventKey),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "schema_version", "Schema version", record => (int)record.SchemaVersion, TransferValueType.Integer),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "subject_type", "Subject type", record => record.SubjectType),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "subject_id", "Subject id", record => record.SubjectId),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "occurred_at", "Occurred at", record => record.OccurredAt, TransferValueType.DateTime),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "origin_type", "Origin", record => record.OriginType, TransferValueType.Enum),
            ArchiveField.Text<EventRecord>(TransferRecordTypes.Event, "payload", "Payload", record => record.Payload, TransferValueType.Json),
        ],
    };

    public static IReadOnlyList<IArchiveRecordDefinition> InDependencyOrder { get; } =
    [
        Workspace,
        Member,
        Status,
        Tag,
        RelationType,
        Project,
        Board,
        BoardGroup,
        Sprint,
        Task,
        TaskAssignee,
        TaskTag,
        TaskPlacement,
        TaskRelation,
        Comment,
        Reaction,
        Flag,
        Automation,
        WorkspaceFile,
        Event,
    ];

    public static IReadOnlyList<TransferRecordType> RecordTypes { get; } = InDependencyOrder
        .Select(definition => definition.RecordType)
        .ToList();

    private static string MemberEmail(WorkspaceAppUser member)
    {
        return UserEmail(member.User);
    }

    private static string UserEmail(AppUser? user)
    {
        return user?.NormalizedEmail ?? user?.Email ?? user?.UserName ?? EntityRefBuilder.UnnamedSegment;
    }

    private static EntityRef? AuthorRef(AppUser? user)
    {
        if (user is null)
        {
            return null;
        }

        return EntityRefBuilder.ForUser(UserEmail(user));
    }

    private static string BoardIdentifier(BoardGroup group)
    {
        return group.Board?.Identifier ?? EntityRefBuilder.UnnamedSegment;
    }

    private static string ProjectKey(Project? project)
    {
        return project?.Key ?? EntityRefBuilder.UnnamedSegment;
    }

    private static EntityRef? SprintRef(Sprint? sprint)
    {
        if (sprint is null)
        {
            return null;
        }

        return EntityRefBuilder.ForSprint(ProjectKey(sprint.Project), sprint.Name);
    }

    private static EntityRef? BoardGroupRef(BoardGroup? group)
    {
        if (group is null)
        {
            return null;
        }

        return EntityRefBuilder.ForBoardGroup(BoardIdentifier(group), group.Name);
    }

    private static EntityRef? TaskRef(ProjectTask? task)
    {
        if (task is null)
        {
            return null;
        }

        return EntityRefBuilder.ForTask(task.Project?.Key ?? EntityRefBuilder.UnnamedSegment, task.ProjectScopeId);
    }

    private static string TaskRefValue(ProjectTask? task)
    {
        return TaskRef(task)?.Value ?? EntityRefBuilder.UnnamedSegment;
    }
}
