using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Authorization;
using Netptune.Core.BaseEntities;
using Netptune.Core.Constants;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Core.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.Transfer;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;

namespace Netptune.IntegrationTests.Endpoints;

// Rows are authored by a workspace service account, so anything the teardown misses also blocks
// the service-account user delete, not just the workspace delete.
internal sealed class WorkspaceRowSeeder
{
    private readonly DataContext Context;
    private readonly int WorkspaceId;
    private readonly string ActorId;
    private readonly CancellationToken CancellationToken;
    private readonly List<string> StorageKeys = [];

    private WorkspaceRowSeeder(
        DataContext context,
        int workspaceId,
        string actorId,
        CancellationToken cancellationToken)
    {
        Context = context;
        WorkspaceId = workspaceId;
        ActorId = actorId;
        CancellationToken = cancellationToken;
    }

    public static async Task<SeededWorkspace> Seed(
        DataContext context,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var actorId = await AddServiceAccount(context, workspaceId, cancellationToken);
        var seeder = new WorkspaceRowSeeder(context, workspaceId, actorId, cancellationToken);

        await seeder.AddFeatureRows();

        return new SeededWorkspace(actorId, seeder.StorageKeys);
    }

    private async Task AddFeatureRows()
    {
        var project = await Context.Projects.FirstAsync(x => x.WorkspaceId == WorkspaceId, CancellationToken);
        var status = await Context.Statuses.FirstAsync(x => x.WorkspaceId == WorkspaceId, CancellationToken);
        var relationType = await Context.RelationTypes.FirstAsync(x => x.WorkspaceId == WorkspaceId, CancellationToken);

        var tasks = await AddTasks(project, status, relationType);

        await AddBoardsAndSprints(project, status, tasks[0]);
        await AddComments(tasks[0]);
        await AddAutomation(project);
        await AddEvents(tasks[0]);
        await AddPages(project);
        await AddAssistantHistory(tasks[0]);
        await AddTransfers();
        await AddSourceControl(project, tasks[0]);
        await AddFiles(tasks[0]);
        await AddPreferences();
    }

    private static async Task<string> AddServiceAccount(
        DataContext context,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var user = new AppUser
        {
            UserName = $"teardown-{Guid.NewGuid():N}@service.netptune.co.uk",
            Email = $"teardown-{Guid.NewGuid():N}@service.netptune.co.uk",
            Firstname = "Teardown",
            Lastname = "Account",
            UserType = AppUserType.ServiceAccount,
        };

        context.AppUsers.Add(user);
        context.WorkspaceAppUsers.Add(new WorkspaceAppUser
        {
            WorkspaceId = workspaceId,
            UserId = user.Id,
            Role = WorkspaceRole.Member,
            Permissions = [],
        });
        context.ServiceAccounts.Add(new ServiceAccount
        {
            UserId = user.Id,
            WorkspaceId = workspaceId,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    private async Task<List<ProjectTask>> AddTasks(Project project, Status status, RelationType relationType)
    {
        var tasks = new List<ProjectTask>
        {
            NewTask(project, status, 9001),
            NewTask(project, status, 9002),
        };

        var tag = Authored(new Tag { Name = "teardown" });

        Context.ProjectTasks.AddRange(tasks);
        Context.Tags.Add(tag);

        await Context.SaveChangesAsync(CancellationToken);

        Context.ProjectTaskTags.Add(new ProjectTaskTag { ProjectTaskId = tasks[0].Id, TagId = tag.Id });
        Context.ProjectTaskAppUsers.Add(new ProjectTaskAppUser { ProjectTaskId = tasks[0].Id, UserId = ActorId });
        Context.ProjectTaskRelations.Add(new ProjectTaskRelation
        {
            WorkspaceId = WorkspaceId,
            RelationTypeId = relationType.Id,
            SourceTaskId = tasks[0].Id,
            TargetTaskId = tasks[1].Id,
        });
        Context.TaskPins.Add(Authored(new TaskPin
        {
            ProjectTaskId = tasks[0].Id,
            Scope = TaskPinScope.User,
            ScopeEntityId = 1,
        }));

        await Context.SaveChangesAsync(CancellationToken);

        return tasks;
    }

    private async Task AddBoardsAndSprints(Project project, Status status, ProjectTask task)
    {
        var board = Authored(new Board
        {
            Name = "Teardown board",
            Identifier = $"teardown-{Guid.NewGuid():N}",
            ProjectId = project.Id,
            MetaInfo = new BoardMeta(),
        });

        Context.Boards.Add(board);
        Context.Sprints.Add(Authored(new Sprint
        {
            Name = "Teardown sprint",
            Identifier = $"teardown-{Guid.NewGuid():N}",
            ProjectId = project.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
        }));

        await Context.SaveChangesAsync(CancellationToken);

        var group = Authored(new BoardGroup { Name = "Teardown group", BoardId = board.Id, StatusId = status.Id });

        Context.BoardGroups.Add(group);

        await Context.SaveChangesAsync(CancellationToken);

        Context.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            ProjectTaskId = task.Id,
            BoardGroupId = group.Id,
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddComments(ProjectTask task)
    {
        var comment = Authored(new Comment { Body = "Teardown", EntityId = task.Id, EntityType = EntityType.Task });

        Context.Comments.Add(comment);

        await Context.SaveChangesAsync(CancellationToken);

        Context.Reactions.Add(Authored(new Reaction { CommentId = comment.Id, Value = ReactionValues.ThumbsUp }));
        Context.CommentMentions.Add(Authored(new CommentMention { CommentId = comment.Id, UserId = ActorId }));

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddAutomation(Project project)
    {
        var rule = Authored(new AutomationRule
        {
            Name = "Teardown rule",
            TriggerType = AutomationTriggerType.TaskUnassignedFor,
            ExecutionUserId = ActorId,
            ProjectId = project.Id,
        });

        Context.AutomationRules.Add(rule);

        await Context.SaveChangesAsync(CancellationToken);

        Context.Flags.Add(Authored(new Flag
        {
            Name = "Teardown",
            Description = "Teardown",
            AutomationRuleId = rule.Id,
        }));

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddEvents(ProjectTask task)
    {
        var eventRecord = new EventRecord
        {
            WorkspaceId = WorkspaceId,
            EventKey = "task.created",
            SubjectType = "task",
            SubjectId = task.Id.ToString(),
            ActorUserId = ActorId,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
        };

        Context.EventRecords.Add(eventRecord);
        Context.EventStreamHeads.Add(new EventStreamHead
        {
            WorkspaceId = WorkspaceId,
            SubjectType = "task",
            SubjectId = task.Id.ToString(),
            CurrentSequence = 1,
        });

        await Context.SaveChangesAsync(CancellationToken);

        Context.ActivityEntries.Add(Authored(new ActivityEntry
        {
            EntityType = EntityType.Task,
            EntityId = task.Id,
            UserId = ActorId,
            LastEventRecordId = eventRecord.Id,
            FirstOccurredAt = DateTime.UtcNow,
            LastOccurredAt = DateTime.UtcNow,
            WindowExpiresAt = DateTime.UtcNow,
        }));
        Context.Notifications.Add(new Notification
        {
            UserId = ActorId,
            EventRecordId = eventRecord.Id,
            WorkspaceId = WorkspaceId,
            EntityType = EntityType.Task,
            OwnerId = ActorId,
            CreatedByUserId = ActorId,
        });
        Context.NotificationSubscriptions.Add(Authored(new NotificationSubscription
        {
            UserId = ActorId,
            Scope = NotificationScope.Project,
            ScopeEntityId = task.ProjectId!.Value,
        }));

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddPages(Project project)
    {
        var parent = Authored(new Page
        {
            Kind = PageKind.Document,
            Title = "Teardown parent",
            Slug = $"teardown-{Guid.NewGuid():N}",
            ProjectId = project.Id,
            ReviewedByUserId = ActorId,
        });

        Context.Pages.Add(parent);

        await Context.SaveChangesAsync(CancellationToken);

        var child = Authored(new Page
        {
            Kind = PageKind.Document,
            Title = "Teardown child",
            Slug = $"teardown-{Guid.NewGuid():N}",
            ParentId = parent.Id,
        });

        Context.Pages.Add(child);
        Context.PageVersions.Add(new PageVersion
        {
            PageId = parent.Id,
            Revision = 1,
            Title = parent.Title,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = ActorId,
        });
        Context.PageLinks.Add(new PageLink
        {
            PageId = parent.Id,
            TargetType = PageLinkTargetType.Task,
            TargetId = "1",
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddAssistantHistory(ProjectTask task)
    {
        var conversation = Authored(new AiConversation
        {
            Id = Guid.NewGuid(),
            UserId = ActorId,
            Title = "Teardown",
            Model = "claude-opus-5-5",
            LastMessageAt = DateTime.UtcNow,
        });

        Context.AiConversations.Add(conversation);

        await Context.SaveChangesAsync(CancellationToken);

        var message = new AiMessage
        {
            ConversationId = conversation.Id,
            Model = conversation.Model,
            CreatedAt = DateTime.UtcNow,
        };

        Context.AiMessages.Add(message);

        await Context.SaveChangesAsync(CancellationToken);

        var changeSet = Authored(new AiChangeSet
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            MessageId = message.Id,
            UserId = ActorId,
            CorrelationId = Guid.NewGuid(),
        });

        Context.AiChangeSets.Add(changeSet);
        Context.AiReviewRuns.Add(Authored(new AiReviewRun
        {
            SubjectType = EntityType.Task,
            SubjectId = task.Id,
            CorrelationId = Guid.NewGuid(),
            ReviewedFingerprint = new string('a', 64),
            BaselineFingerprint = new string('a', 64),
            Model = conversation.Model,
            StartedAt = DateTime.UtcNow,
        }));
        Context.AiUsageRecords.Add(new AiUsageRecord
        {
            WorkspaceId = WorkspaceId,
            UserId = ActorId,
            Model = conversation.Model,
            Purpose = "chat",
            CreatedAt = DateTime.UtcNow,
        });
        Context.AiWebDocuments.Add(new AiWebDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = WorkspaceId,
            ConversationId = conversation.Id,
            RequestedUrl = "https://example.com",
            FinalUrl = "https://example.com",
            Content = "Teardown",
            FetchedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        Context.WorkspaceAiCredentials.Add(new WorkspaceAiCredential
        {
            Id = Guid.NewGuid(),
            WorkspaceId = WorkspaceId,
            Label = "Teardown",
            Secret = [1],
            SecretHint = "1",
            CreatedAt = DateTime.UtcNow,
        });
        Context.WorkspaceSearchCredentials.Add(new WorkspaceSearchCredential
        {
            Id = Guid.NewGuid(),
            WorkspaceId = WorkspaceId,
            CreatedByUserId = ActorId,
            CreatedAt = DateTime.UtcNow,
        });

        await Context.SaveChangesAsync(CancellationToken);

        Context.AiProposedChanges.Add(new AiProposedChange
        {
            ChangeSetId = changeSet.Id,
            ToolName = "create_task",
            EntityType = "task",
            Summary = "Teardown",
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddTransfers()
    {
        var definition = JsonDocument.Parse("{}");
        var importSession = Authored(new ImportSession
        {
            OriginalName = "teardown.csv",
            StorageKey = NewStorageKey(),
            TargetRecordType = EntityRefTypes.Task,
            CreatedBy = ActorId,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });

        Context.TaskViews.Add(Authored(new TaskView
        {
            Name = "Teardown",
            Slug = "teardown",
            Definition = definition,
        }));
        Context.ExportDefinitions.Add(Authored(new ExportDefinition
        {
            Name = "Teardown",
            RecordType = EntityRefTypes.Task,
            Definition = definition,
        }));
        Context.ExportJobs.Add(Authored(new ExportJob
        {
            RecordType = EntityRefTypes.Task,
            Definition = definition,
            RequestedBy = ActorId,
            StorageKey = NewStorageKey(),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        }));
        Context.ImportDefinitions.Add(Authored(new ImportDefinition
        {
            Name = "Teardown",
            RecordType = EntityRefTypes.Task,
            Mapping = definition,
        }));
        Context.ImportSessions.Add(importSession);

        await Context.SaveChangesAsync(CancellationToken);

        Context.ImportSessionEntries.Add(new ImportSessionEntry
        {
            SessionId = importSession.Id,
            EntityType = "task",
            EntityId = 1,
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddSourceControl(Project project, ProjectTask task)
    {
        var connection = Authored(new SourceControlConnection
        {
            ExternalAccountId = Guid.NewGuid().ToString("N"),
            AccountLogin = "teardown",
        });

        Context.SourceControlConnections.Add(connection);

        await Context.SaveChangesAsync(CancellationToken);

        var repo = new SourceControlRepo
        {
            WorkspaceId = WorkspaceId,
            ConnectionId = connection.Id,
            ExternalId = Guid.NewGuid().ToString("N"),
            Owner = "teardown",
            Name = "teardown",
            FullName = "teardown/teardown",
            DefaultBranch = "main",
            WebUrl = "https://example.com/teardown",
            SyncedAt = DateTime.UtcNow,
        };

        Context.SourceControlRepos.Add(repo);

        await Context.SaveChangesAsync(CancellationToken);

        Context.SourceControlRepoProjects.Add(new SourceControlRepoProject
        {
            RepoId = repo.Id,
            ProjectId = project.Id,
        });
        Context.SourceControlCheckRuns.Add(new SourceControlCheckRun
        {
            WorkspaceId = WorkspaceId,
            RepoId = repo.Id,
            ExternalId = "1",
            Name = "build",
            CommitSha = new string('a', 40),
            WebUrl = "https://example.com/teardown/checks/1",
            UpdatedAt = DateTime.UtcNow,
        });
        Context.TaskCodeLinks.Add(new TaskCodeLink
        {
            WorkspaceId = WorkspaceId,
            TaskId = task.Id,
            RepoId = repo.Id,
            ExternalRef = "teardown",
            Title = "teardown",
            WebUrl = "https://example.com/teardown/tree/teardown",
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddFiles(ProjectTask task)
    {
        var file = Authored(new WorkspaceFile
        {
            OriginalName = "teardown.txt",
            StorageKey = NewStorageKey(),
            SizeBytes = 1,
        });

        Context.WorkspaceFiles.Add(file);

        await Context.SaveChangesAsync(CancellationToken);

        Context.TaskFiles.Add(new TaskFile
        {
            WorkspaceId = WorkspaceId,
            ProjectTaskId = task.Id,
            WorkspaceFileId = file.Id,
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private async Task AddPreferences()
    {
        Context.CommandPaletteRecentItems.Add(new CommandPaletteRecentItem
        {
            UserId = ActorId,
            WorkspaceId = WorkspaceId,
            Type = "task",
            Title = "Teardown",
            Url = "/teardown",
            LastAccessedAt = DateTime.UtcNow,
        });
        Context.UserPreferenceValues.Add(new UserPreferenceValue
        {
            UserId = ActorId,
            WorkspaceId = WorkspaceId,
            Key = "teardown",
            Value = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        Context.WorkspaceInvites.Add(new WorkspaceInvite
        {
            Email = "teardown@example.com",
            WorkspaceId = WorkspaceId,
            Code = Guid.NewGuid().ToString("N"),
            InvitedByUserId = ActorId,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });

        await Context.SaveChangesAsync(CancellationToken);
    }

    private string NewStorageKey()
    {
        var key = $"teardown/{Guid.NewGuid():N}";

        StorageKeys.Add(key);

        return key;
    }

    private ProjectTask NewTask(Project project, Status status, int projectScopeId)
    {
        return Authored(new ProjectTask
        {
            Name = $"Teardown {projectScopeId}",
            ProjectId = project.Id,
            StatusId = status.Id,
            ProjectScopeId = projectScopeId,
        });
    }

    private TEntity Authored<TEntity>(TEntity entity) where TEntity : IWorkspaceEntity
    {
        entity.WorkspaceId = WorkspaceId;
        entity.OwnerId = ActorId;
        entity.CreatedByUserId = ActorId;

        return entity;
    }
}

internal sealed record SeededWorkspace(string ServiceAccountUserId, IReadOnlyList<string> StorageKeys);
