using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Catalog;
using Netptune.Transfer.Services;

namespace Netptune.Import.Archive;

public sealed class ArchiveImporter : IArchiveImporter
{
    // Records added between saves. Small enough to keep the change tracker and the pending-ref list
    // bounded on a large archive, large enough that the round trips stop dominating.
    private const int BatchSize = 500;

    private readonly INetptuneUnitOfWork UnitOfWork;

    public ArchiveImporter(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<ArchiveImportPreview> Preview(ArchiveImportRequest request, CancellationToken cancellationToken = default)
    {
        using var reader = new ArchiveReader(request.Archive);

        var upgrade = ArchiveSchemaUpgrader.Upgrade(reader.ReadManifest());
        var manifest = upgrade.Manifest;
        var blockers = new List<string>();

        ValidateContentOrder(manifest, blockers);

        var unmatched = await UnmatchedMembers(reader, cancellationToken);
        var counts = manifest.Contents.ToDictionary(content => content.Type, content => content.Count);
        var quota = await RemainingQuota(request, cancellationToken);

        await ValidateTarget(request, blockers, cancellationToken);

        if (manifest.FileBytes > quota)
        {
            blockers.Add($"The archive carries {manifest.FileBytes} bytes of files but only {quota} bytes of storage remain.");
        }

        return new ArchiveImportPreview
        {
            Manifest = manifest,
            CountsByType = counts,
            UnmatchedMemberEmails = unmatched,
            FileBytes = manifest.FileBytes,
            RemainingQuotaBytes = quota,
            SchemaUpgrades = upgrade.Applied,
            Blockers = blockers,
        };
    }

    public async Task<ArchiveImportResult> Import(ArchiveImportRequest request, CancellationToken cancellationToken = default)
    {
        using var reader = new ArchiveReader(request.Archive);

        var upgrade = ArchiveSchemaUpgrader.Upgrade(reader.ReadManifest());
        var manifest = upgrade.Manifest;
        var blockers = new List<string>();

        ValidateContentOrder(manifest, blockers);

        await ValidateTarget(request, blockers, cancellationToken);

        if (blockers.Count > 0)
        {
            throw new ArchiveSchemaException(string.Join(" ", blockers));
        }

        // One transaction for the whole archive. Sections write as they are read, so a failure part way
        // through would otherwise leave a half populated workspace — or, when cloning, an orphan one.
        return await UnitOfWork.Transaction(async () =>
        {
            var workspace = await ResolveWorkspace(request, reader, cancellationToken);
            var context = new ArchiveImportContext(workspace.Id, request.UserId);
            var warnings = new List<string>();

            await ImportMembers(reader, context, warnings, cancellationToken);
            await ImportStatuses(reader, context, cancellationToken);
            await ImportTags(reader, context, cancellationToken);
            await ImportRelationTypes(reader, context, cancellationToken);
            await ImportProjects(reader, context, cancellationToken);
            await ImportBoards(reader, context, cancellationToken);
            await ImportBoardGroups(reader, context, cancellationToken);
            await ImportSprints(reader, context, cancellationToken);

            var tasks = await ImportTasks(reader, context, cancellationToken);

            await ImportTaskAssignees(reader, context, tasks, cancellationToken);
            await ImportTaskTags(reader, context, cancellationToken);
            await ImportTaskPlacements(reader, context, cancellationToken);
            await ImportTaskRelations(reader, context, warnings, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return new ArchiveImportResult
            {
                WorkspaceId = workspace.Id,
                WorkspaceSlug = workspace.Slug,
                CreatedByType = context.CreatedByType,
                Warnings = warnings,
            };
        });
    }

    // The applier walks the manifest in the order it declares, so an archive whose order violates the
    // dependency graph would resolve references before their targets exist. Refuse it instead.
    private static void ValidateContentOrder(ArchiveManifest manifest, List<string> blockers)
    {
        var expected = ArchiveCatalog.InDependencyOrder.Select(definition => definition.FileName).ToList();
        var positions = manifest.Contents
            .Select(content => expected.IndexOf(content.File))
            .ToList();

        if (positions.Any(position => position < 0))
        {
            blockers.Add("The archive declares a data file this build does not know how to read.");

            return;
        }

        var isOrdered = positions.Zip(positions.Skip(1), (left, right) => left < right).All(ordered => ordered);

        if (!isOrdered)
        {
            blockers.Add("The archive lists its data files out of dependency order.");
        }
    }

    private async Task ValidateTarget(ArchiveImportRequest request, List<string> blockers, CancellationToken cancellationToken)
    {
        if (request.Mode == ArchiveImportMode.Clone)
        {
            if (string.IsNullOrWhiteSpace(request.TargetSlug))
            {
                blockers.Add("Cloning an archive needs a slug for the new workspace.");

                return;
            }

            var existingId = await UnitOfWork.Workspaces.GetIdBySlug(request.TargetSlug, cancellationToken);

            if (existingId is not null)
            {
                blockers.Add($"A workspace with the slug '{request.TargetSlug}' already exists.");
            }

            return;
        }

        if (request.WorkspaceId is null)
        {
            blockers.Add("Restoring an archive needs a target workspace.");

            return;
        }

        var projects = await UnitOfWork.Projects.GetAllInWorkspace(request.WorkspaceId.Value, cancellationToken: cancellationToken);

        if (projects.Count > 0)
        {
            blockers.Add("Restore only works into an empty workspace. This one already has projects.");
        }
    }

    private async Task<Workspace> ResolveWorkspace(ArchiveImportRequest request, ArchiveReader reader, CancellationToken cancellationToken)
    {
        if (request.Mode == ArchiveImportMode.Restore)
        {
            return await UnitOfWork.Workspaces.GetAsync(request.WorkspaceId!.Value, cancellationToken: cancellationToken)
                ?? throw new ArchiveSchemaException("The target workspace could not be resolved.");
        }

        var source = await First(reader, ArchiveCatalog.Workspace.FileName, cancellationToken);
        var workspace = new Workspace
        {
            Slug = request.TargetSlug!,
            Name = source?.Text("name") ?? request.TargetSlug!,
            Description = source?.Text("description"),
            MetaInfo = new Core.Meta.WorkspaceMeta(),
            OwnerId = request.UserId,
            CreatedByUserId = request.UserId,
        };

        await UnitOfWork.Workspaces.AddAsync(workspace, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        await UnitOfWork.WorkspaceUsers.AddAsync(new WorkspaceAppUser
        {
            WorkspaceId = workspace.Id,
            UserId = request.UserId,
            Role = Core.Authorization.WorkspaceRole.Owner,
            Permissions = Core.Authorization.WorkspaceRolePermissions
                .GetDefaultPermissions(Core.Authorization.WorkspaceRole.Owner)
                .ToList(),
        }, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return workspace;
    }

    private async Task ImportMembers(
        ArchiveReader reader,
        ArchiveImportContext context,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        await foreach (var row in reader.ReadSection(ArchiveCatalog.Member.FileName, cancellationToken))
        {
            var email = row.Text("email");

            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var user = await UnitOfWork.Users.GetByEmail(email, true, cancellationToken);

            if (user is null)
            {
                warnings.Add($"'{email}' is not a Netptune user and was not added to the workspace.");
                continue;
            }

            context.Register(row.Ref, 0);
            context.RegisterUser(row.Ref, user.Id);
        }
    }

    // Adds a section's records in batches. The ref map needs the ids the database generates, so each
    // batch is saved and only then registered — but saving once per record, as this used to, turned a
    // large archive into one round trip per row.
    private async Task ImportSection<TEntity>(
        ArchiveReader reader,
        string fileName,
        ArchiveImportContext context,
        Func<ArchiveRow, Task<TEntity?>> add,
        Func<TEntity, int> readId,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var pending = new List<(EntityRef Ref, TEntity Entity)>(BatchSize);

        await foreach (var row in reader.ReadSection(fileName, cancellationToken))
        {
            var entity = await add(row);

            if (entity is null)
            {
                continue;
            }

            pending.Add((row.Ref, entity));

            if (pending.Count < BatchSize)
            {
                continue;
            }

            await RegisterBatch(pending, context, readId, cancellationToken);
        }

        await RegisterBatch(pending, context, readId, cancellationToken);
    }

    private async Task RegisterBatch<TEntity>(
        List<(EntityRef Ref, TEntity Entity)> pending,
        ArchiveImportContext context,
        Func<TEntity, int> readId,
        CancellationToken cancellationToken)
    {
        if (pending.Count == 0)
        {
            return;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        foreach (var (reference, entity) in pending)
        {
            context.Register(reference, readId(entity));
        }

        pending.Clear();
    }

    private Task ImportStatuses(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.Status.FileName, context, async row =>
        {
            var status = new Status
            {
                WorkspaceId = context.WorkspaceId,
                Key = row.Text("key") ?? row.Ref.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                Description = row.Text("description"),
                Color = row.Text("color"),
                SortOrder = row.Number("sort_order") ?? 0,
                Category = row.Enum<StatusCategory>("category") ?? StatusCategory.Todo,
                EntityType = row.Enum<EntityType>("entity_type") ?? EntityType.Task,
                IsSystem = row.Flag("is_system"),
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.Statuses.AddAsync(status, cancellationToken);

            return status;
        }, status => status.Id, cancellationToken);
    }

    private Task ImportTags(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.Tag.FileName, context, async row =>
        {
            var tag = new Tag
            {
                WorkspaceId = context.WorkspaceId,
                Name = row.Text("name") ?? row.Ref.Value,
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.Tags.AddAsync(tag, cancellationToken);

            return tag;
        }, tag => tag.Id, cancellationToken);
    }

    private Task ImportRelationTypes(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.RelationType.FileName, context, async row =>
        {
            var relationType = new RelationType
            {
                WorkspaceId = context.WorkspaceId,
                Key = row.Text("key") ?? row.Ref.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                InverseName = row.Text("inverse_name") ?? row.Ref.Value,
                Description = row.Text("description"),
                Color = row.Text("color"),
                SortOrder = row.Number("sort_order") ?? 0,
                Category = row.Enum<RelationCategory>("category") ?? RelationCategory.Related,
                IsSystem = row.Flag("is_system"),
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.RelationTypes.AddAsync(relationType, cancellationToken);

            return relationType;
        }, relationType => relationType.Id, cancellationToken);
    }

    private Task ImportProjects(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.Project.FileName, context, async row =>
        {
            var project = new Project
            {
                WorkspaceId = context.WorkspaceId,
                Key = row.Text("key") ?? row.Ref.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                Description = row.Text("description"),
                RepositoryUrl = row.Text("repository_url"),
                MetaInfo = new Core.Meta.ProjectMeta { Color = row.Text("color") },
                DefaultStatusId = context.Resolve(row.Reference("default_status")),
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.Projects.AddAsync(project, cancellationToken);

            return project;
        }, project => project.Id, cancellationToken);
    }

    private Task ImportBoards(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.Board.FileName, context, async row =>
        {
            var projectId = context.Resolve(row.Reference("project"));

            if (projectId is null)
            {
                return null;
            }

            var board = new Board
            {
                WorkspaceId = context.WorkspaceId,
                ProjectId = projectId.Value,
                Identifier = row.Text("identifier") ?? row.Ref.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                BoardType = row.Enum<BoardType>("board_type") ?? BoardType.UserDefined,
                MetaInfo = new Core.Meta.BoardMeta { Color = row.Text("color") },
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.Boards.AddAsync(board, cancellationToken);

            return board;
        }, board => board.Id, cancellationToken);
    }

    private Task ImportBoardGroups(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.BoardGroup.FileName, context, async row =>
        {
            var boardId = context.Resolve(row.Reference("board"));

            if (boardId is null)
            {
                return null;
            }

            var group = new BoardGroup
            {
                WorkspaceId = context.WorkspaceId,
                BoardId = boardId.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                SortOrder = row.Number("sort_order") ?? 0,
                StatusId = context.Resolve(row.Reference("status")),
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.BoardGroups.AddAsync(group, cancellationToken);

            return group;
        }, group => group.Id, cancellationToken);
    }

    private Task ImportSprints(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        return ImportSection(reader, ArchiveCatalog.Sprint.FileName, context, async row =>
        {
            var projectId = context.Resolve(row.Reference("project"));

            if (projectId is null)
            {
                return null;
            }

            var sprint = new Sprint
            {
                WorkspaceId = context.WorkspaceId,
                ProjectId = projectId.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                Goal = row.Text("goal"),
                Status = row.Enum<SprintStatus>("status") ?? SprintStatus.Planning,
                StartDate = row.Timestamp("start_date") ?? DateTime.UtcNow,
                EndDate = row.Timestamp("end_date") ?? DateTime.UtcNow,
                StartedAt = row.Timestamp("started_at"),
                CompletedAt = row.Timestamp("completed_at"),
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            await UnitOfWork.Sprints.AddAsync(sprint, cancellationToken);

            return sprint;
        }, sprint => sprint.Id, cancellationToken);
    }

    private async Task<Dictionary<int, ProjectTask>> ImportTasks(
        ArchiveReader reader,
        ArchiveImportContext context,
        CancellationToken cancellationToken)
    {
        // An archive's tasks come grouped by project, so one lookup covers a long run of rows. The
        // repository issues a real query every time rather than reading the change tracker.
        var projects = new Dictionary<int, Project>();
        var created = new List<ProjectTask>();

        await ImportSection(reader, ArchiveCatalog.Task.FileName, context, async row =>
        {
            var projectId = context.Resolve(row.Reference("project"));
            var statusId = context.Resolve(row.Reference("status"));

            if (projectId is null || statusId is null)
            {
                return null;
            }

            var project = projects.GetValueOrDefault(projectId.Value)
                ?? await UnitOfWork.Projects.GetAsync(projectId.Value, cancellationToken: cancellationToken);

            if (project is null)
            {
                return null;
            }

            projects[projectId.Value] = project;

            // Keep the archived task number: it is the user-facing id, it is what the task's ref is built
            // from, and letting the project allocate a fresh one would silently renumber the workspace.
            var scopeId = row.Integer("scope_id") ?? project.NextTaskScopeId;
            var task = new ProjectTask
            {
                WorkspaceId = context.WorkspaceId,
                ProjectId = projectId.Value,
                ProjectScopeId = scopeId,
                StatusId = statusId.Value,
                Name = row.Text("name") ?? row.Ref.Value,
                Description = row.Text("description"),
                Priority = row.Enum<TaskPriority>("priority"),
                EstimateType = row.Enum<EstimateType>("estimate_type"),
                EstimateValue = row.Decimal("estimate_value"),
                StartDate = row.Date("start_date"),
                DueDate = row.Date("due_date"),
                ExternalId = row.Text("external_id"),
                SprintId = context.Resolve(row.Reference("sprint")),
                OwnerId = context.UserId,
                CreatedByUserId = context.UserId,
            };

            project.NextTaskScopeId = Math.Max(project.NextTaskScopeId, scopeId + 1);

            await UnitOfWork.Tasks.AddAsync(task, cancellationToken);

            created.Add(task);

            return task;
        }, task => task.Id, cancellationToken);

        // Every batch has been saved by now, so the ids are real. The later link sections take their
        // tasks from here rather than querying one back per row.
        return created.ToDictionary(task => task.Id);
    }

    private async Task ImportTaskAssignees(
        ArchiveReader reader,
        ArchiveImportContext context,
        IReadOnlyDictionary<int, ProjectTask> tasks,
        CancellationToken cancellationToken)
    {
        var pending = 0;

        await foreach (var row in reader.ReadSection(ArchiveCatalog.TaskAssignee.FileName, cancellationToken))
        {
            var taskId = context.Resolve(row.Reference("task"));
            var userId = context.ResolveUser(row.Reference("user"));

            if (taskId is null || userId is null)
            {
                continue;
            }

            // There is no repository for the assignee join, so the row has to go on the task's own
            // collection. The task came from the section before this one, so it is already to hand.
            var task = tasks.GetValueOrDefault(taskId.Value);

            if (task is null)
            {
                continue;
            }

            task.ProjectTaskAppUsers.Add(new ProjectTaskAppUser
            {
                ProjectTaskId = taskId.Value,
                UserId = userId,
            });

            context.Count(TransferRecordTypes.TaskAssignee);

            pending = await SaveWhenFull(pending + 1, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    // Keeps a long link section from holding every row in the change tracker at once.
    private async Task<int> SaveWhenFull(int pending, CancellationToken cancellationToken)
    {
        if (pending < BatchSize)
        {
            return pending;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return 0;
    }

    private async Task ImportTaskTags(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        var pending = 0;

        await foreach (var row in reader.ReadSection(ArchiveCatalog.TaskTag.FileName, cancellationToken))
        {
            var taskId = context.Resolve(row.Reference("task"));
            var tagId = context.Resolve(row.Reference("tag"));

            if (taskId is null || tagId is null)
            {
                continue;
            }

            await UnitOfWork.ProjectTaskTags.AddAsync(new ProjectTaskTag
            {
                ProjectTaskId = taskId.Value,
                TagId = tagId.Value,
            }, cancellationToken);

            context.Count(TransferRecordTypes.TaskTag);

            pending = await SaveWhenFull(pending + 1, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task ImportTaskPlacements(ArchiveReader reader, ArchiveImportContext context, CancellationToken cancellationToken)
    {
        var pending = 0;

        await foreach (var row in reader.ReadSection(ArchiveCatalog.TaskPlacement.FileName, cancellationToken))
        {
            var taskId = context.Resolve(row.Reference("task"));
            var groupId = context.Resolve(row.Reference("board_group"));

            if (taskId is null || groupId is null)
            {
                continue;
            }

            await UnitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
            {
                ProjectTaskId = taskId.Value,
                BoardGroupId = groupId.Value,
                SortOrder = row.Number("sort_order") ?? 0,
            }, cancellationToken);

            context.Count(TransferRecordTypes.TaskPlacement);

            pending = await SaveWhenFull(pending + 1, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task ImportTaskRelations(
        ArchiveReader reader,
        ArchiveImportContext context,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var pending = 0;

        await foreach (var row in reader.ReadSection(ArchiveCatalog.TaskRelation.FileName, cancellationToken))
        {
            var sourceId = context.Resolve(row.Reference("source"));
            var targetId = context.Resolve(row.Reference("target"));
            var relationTypeId = context.Resolve(row.Reference("relation_type"));

            if (sourceId is null || targetId is null || relationTypeId is null)
            {
                warnings.Add($"Relation {row.Ref} referenced something the archive did not carry.");
                continue;
            }

            await UnitOfWork.ProjectTaskRelations.AddAsync(new ProjectTaskRelation
            {
                WorkspaceId = context.WorkspaceId,
                SourceTaskId = sourceId.Value,
                TargetTaskId = targetId.Value,
                RelationTypeId = relationTypeId.Value,
            }, cancellationToken);

            context.Count(TransferRecordTypes.TaskRelation);

            pending = await SaveWhenFull(pending + 1, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task<List<string>> UnmatchedMembers(ArchiveReader reader, CancellationToken cancellationToken)
    {
        var unmatched = new List<string>();

        await foreach (var row in reader.ReadSection(ArchiveCatalog.Member.FileName, cancellationToken))
        {
            var email = row.Text("email");

            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var user = await UnitOfWork.Users.GetByEmail(email, true, cancellationToken);

            if (user is null)
            {
                unmatched.Add(email);
            }
        }

        return unmatched;
    }

    private async Task<long> RemainingQuota(ArchiveImportRequest request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId is null)
        {
            return long.MaxValue;
        }

        var usage = await UnitOfWork.Workspaces.GetStorageUsage(request.WorkspaceId.Value, cancellationToken);

        return usage?.AvailableBytes ?? long.MaxValue;
    }

    private static async Task<ArchiveRow?> First(ArchiveReader reader, string fileName, CancellationToken cancellationToken)
    {
        await foreach (var row in reader.ReadSection(fileName, cancellationToken))
        {
            return row;
        }

        return null;
    }
}
