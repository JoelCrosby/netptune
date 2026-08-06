using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Transfer.Services;
using Netptune.Transfer.Import;
using Netptune.Core.UnitOfWork;

namespace Netptune.Import;

public sealed class ImportApplier : IImportApplier
{
    private const int BatchSize = 500;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IEnumerable<IImportSourceReader> Readers;
    private readonly IEventRecordWriter EventRecords;
    private readonly IEventPublisher EventPublisher;
    private readonly IActivityLogger? Activity;

    public ImportApplier(
        INetptuneUnitOfWork unitOfWork,
        IEnumerable<IImportSourceReader> readers,
        IEventRecordWriter eventRecords,
        IEventPublisher eventPublisher,
        IImportSessionRepository importSessions,
        // Optional because the job server has no HTTP request to take an actor from, the same reason
        // TaskMutationPipeline treats it as optional.
        IActivityLogger? activity = null)
    {
        UnitOfWork = unitOfWork;
        Readers = readers;
        EventRecords = eventRecords;
        EventPublisher = eventPublisher;
        Activity = activity;
        ImportSessions = importSessions;
    }

    public async Task<ImportPreviewResult> Preview(ImportApplyRequest request, CancellationToken cancellationToken = default)
    {
        var context = await LoadContext(request, cancellationToken);
        var resolver = new ImportRowResolver(request.Mapping, request.ColumnNames);
        var diagnostics = new List<ImportRowDiagnostic>();
        var samples = new List<ImportRowPreview>();
        var newTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var counts = new ActionCounts();
        var totalRows = 0;
        var truncated = false;

        await foreach (var row in ReadRows(request, cancellationToken))
        {
            if (totalRows >= request.PreviewRowCap)
            {
                truncated = true;
                break;
            }

            totalRows++;

            var resolved = resolver.Resolve(row);
            var action = ResolveAction(resolved, request, context);

            counts.Add(action);
            CollectVocabularyGaps(resolved, request.Mapping, context, newTags, newGroups, invites, resolved.Diagnostics);

            foreach (var diagnostic in resolved.Diagnostics)
            {
                if (diagnostics.Count < ImportPreviewResult.MaxDiagnostics)
                {
                    diagnostics.Add(diagnostic);
                }
            }

            if (samples.Count < ImportPreviewResult.MaxSampleRows)
            {
                samples.Add(ToSample(resolved, action));
            }
        }

        return new ImportPreviewResult
        {
            TotalRows = totalRows,
            WillCreate = counts.Created,
            WillUpdate = counts.Updated,
            WillSkip = counts.Skipped,
            WillError = counts.Failed,
            IsExtrapolated = truncated,
            Diagnostics = diagnostics,
            NewEntities = BuildNewEntities(newTags, newGroups),
            UsersToInvite = invites.ToList(),
            SampleRows = samples,
        };
    }

    public async Task<ImportCommitResult> Commit(
        ImportApplyRequest request,
        ImportProgressReporter reportProgress,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContext(request, cancellationToken);
        var resolver = new ImportRowResolver(request.Mapping, request.ColumnNames);
        var counts = new ActionCounts();
        var batch = new List<ResolvedTaskRow>(BatchSize);
        var createdTaskIds = new List<int>();
        var processed = 0;

        await foreach (var row in ReadRows(request, cancellationToken))
        {
            if (processed >= request.MaxRows)
            {
                break;
            }

            processed++;
            batch.Add(resolver.Resolve(row));

            if (batch.Count < BatchSize)
            {
                continue;
            }

            await ApplyBatch(batch, request, context, counts, createdTaskIds, cancellationToken);
            await reportProgress(new ImportProgress
            {
                Percent = PercentDone(processed, request),
                Message = $"Imported {processed} rows",
            }, cancellationToken);

            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await ApplyBatch(batch, request, context, counts, createdTaskIds, cancellationToken);
        }

        await Announce(createdTaskIds, request, cancellationToken);

        return new ImportCommitResult
        {
            Created = counts.Created,
            Updated = counts.Updated,
            Skipped = counts.Skipped,
            Failed = counts.Failed,
        };
    }

    // Capped below 100 so the job handler's own "Complete" is what closes the bar out. Falls back to a
    // fraction of the row cap when the file was never profiled, which still moves rather than sticking.
    private static int PercentDone(int processed, ImportApplyRequest request)
    {
        var total = request.EstimatedRowCount is > 0
            ? Math.Min(request.EstimatedRowCount.Value, request.MaxRows)
            : request.MaxRows;

        return (int) Math.Clamp(processed * 95L / total, 1, 95);
    }

    private async Task ApplyBatch(
        IReadOnlyList<ResolvedTaskRow> batch,
        ImportApplyRequest request,
        ImportContext context,
        ActionCounts counts,
        List<int> createdTaskIds,
        CancellationToken cancellationToken)
    {
        var writable = batch.Where(row => !row.HasErrors || request.SkipFailingRows).ToList();
        var failed = batch.Count - writable.Count;

        counts.Failed += failed;

        var planned = writable
            .Select(row => new PlannedRow(row, ResolveAction(row, request, context)))
            .ToList();
        var createCount = planned.Count(entry => entry.Action == ImportRowAction.Create);

        // Reserved outside the transaction on purpose: a rolled back batch should leave a gap in the
        // task numbers rather than hand the same ones to the next batch.
        await context.ReserveScopeIds(UnitOfWork, createCount, cancellationToken);

        await UnitOfWork.Transaction(async () =>
        {
            var entries = new List<ImportSessionEntry>();
            var creations = new List<Creation>();
            var updates = new List<Update>();

            foreach (var (row, action) in planned)
            {
                if (action == ImportRowAction.Skip || action == ImportRowAction.Error)
                {
                    counts.Add(action);
                    continue;
                }

                if (action == ImportRowAction.Update)
                {
                    var update = PrepareUpdate(row, context, request);

                    if (update is null)
                    {
                        continue;
                    }

                    updates.Add(update);
                    counts.Updated++;
                    continue;
                }

                creations.Add(await PrepareCreate(row, context, request, cancellationToken));
                counts.Created++;
            }

            // One save for the whole batch. It gives every new task the id its board placement, tags and
            // created event need, and stamps a fresh UpdatedAt on the tasks that changed. Saving per row
            // meant a round trip per row.
            await UnitOfWork.CompleteAsync(cancellationToken);

            foreach (var update in updates)
            {
                // Read back after the save: undo compares this against the task's UpdatedAt to tell an
                // outside edit from the import's own write.
                update.Entry.EntityUpdatedAt = update.Task.UpdatedAt;

                entries.Add(update.Entry);
            }

            foreach (var creation in creations)
            {
                await PlaceInBoardGroup(creation.Task, creation.Row, context, request, cancellationToken);
                await ApplyTags(creation.Task, creation.Row, context, request, cancellationToken);
                await WriteCreatedEvent(creation.Task, creation.Status, context, request, cancellationToken);

                createdTaskIds.Add(creation.Task.Id);

                entries.Add(new ImportSessionEntry
                {
                    SessionId = request.Session.Id,
                    EntityType = EventEntityTypes.From(EntityType.Task),
                    EntityId = creation.Task.Id,
                    Operation = ImportEntryOperation.Created,
                });
            }

            await ImportSessions.AddEntries(entries, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });
    }

    private async Task<Creation> PrepareCreate(
        ResolvedTaskRow row,
        ImportContext context,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        var status = ResolveStatus(row, context, request.Mapping);
        var scopeId = context.NextScopeId();
        var task = new ProjectTask
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = context.Project.Id,
            ProjectScopeId = scopeId,
            Name = row.Name,
            Description = row.Description,
            StatusId = status.Id,
            Priority = row.Priority,
            EstimateValue = row.EstimateValue,
            StartDate = row.StartDate,
            DueDate = row.DueDate,
            ExternalId = row.SourceId,
            SprintId = context.Vocabulary.FindSprint(row.SprintValue)?.Id,
            OwnerId = request.UserId,
            CreatedByUserId = request.UserId,
        };

        task.ProjectTaskAppUsers = ResolveAssignees(row, context);

        await UnitOfWork.Tasks.AddAsync(task, cancellationToken);

        return new Creation(row, task, status);
    }

    // Mutates the tracked task and hands back the undo entry to be completed once the batch is saved.
    private static Update? PrepareUpdate(
        ResolvedTaskRow row,
        ImportContext context,
        ImportApplyRequest request)
    {
        var existing = context.FindExisting(row);

        if (existing is null)
        {
            return null;
        }

        var previous = JsonSerializer.SerializeToDocument(new
        {
            existing.Name,
            existing.Description,
            existing.StatusId,
            existing.Priority,
            existing.EstimateValue,
            existing.StartDate,
            existing.DueDate,
        }, JsonOptions.Default);

        existing.Name = row.Name;
        existing.Description = row.Description ?? existing.Description;
        existing.StatusId = ResolveStatus(row, context, request.Mapping).Id;
        existing.Priority = row.Priority ?? existing.Priority;
        existing.EstimateValue = row.EstimateValue ?? existing.EstimateValue;
        existing.StartDate = row.StartDate ?? existing.StartDate;
        existing.DueDate = row.DueDate ?? existing.DueDate;
        existing.ModifiedByUserId = request.UserId;

        var entry = new ImportSessionEntry
        {
            SessionId = request.Session.Id,
            EntityType = EventEntityTypes.From(EntityType.Task),
            EntityId = existing.Id,
            Operation = ImportEntryOperation.Updated,
            PreviousValues = previous,
        };

        return new Update(entry, existing);
    }

    private async Task PlaceInBoardGroup(
        ProjectTask task,
        ResolvedTaskRow row,
        ImportContext context,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        var groupName = row.BoardGroupValue ?? request.Mapping.Defaults.BoardGroupName;
        var group = context.Vocabulary.FindBoardGroup(groupName);

        if (group is null && groupName is not null && request.Mapping.UnknownPolicy.BoardGroups == ImportUnknownPolicy.Create)
        {
            group = await context.CreateBoardGroup(UnitOfWork, groupName, request, cancellationToken);
        }

        group ??= context.DefaultBoardGroup;

        if (group is null)
        {
            return;
        }

        await UnitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
        {
            ProjectTaskId = task.Id,
            BoardGroupId = group.Id,
            SortOrder = context.NextSortOrder(group.Id),
        }, cancellationToken);
    }

    private async Task ApplyTags(
        ProjectTask task,
        ResolvedTaskRow row,
        ImportContext context,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var tagValue in row.TagValues)
        {
            var tag = context.Vocabulary.FindTag(tagValue);

            if (tag is null && request.Mapping.UnknownPolicy.Tags == ImportUnknownPolicy.Create)
            {
                tag = await context.CreateTag(UnitOfWork, tagValue, request, cancellationToken);
            }

            if (tag is null)
            {
                continue;
            }

            await UnitOfWork.ProjectTaskTags.AddAsync(new ProjectTaskTag
            {
                ProjectTaskId = task.Id,
                TagId = tag.Id,
            }, cancellationToken);
        }
    }

    private static List<ProjectTaskAppUser> ResolveAssignees(ResolvedTaskRow row, ImportContext context)
    {
        return row.AssigneeValues
            .Select(context.Vocabulary.FindUser)
            .Where(user => user is not null)
            .Select(user => new ProjectTaskAppUser { UserId = user!.Id })
            .ToList();
    }

    private async Task WriteCreatedEvent(
        ProjectTask task,
        Status status,
        ImportContext context,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        await EventRecords.Append(new EventWriteRequest<EntityCreatedPayload>
        {
            WorkspaceId = request.WorkspaceId,
            EventKey = EventKeys.EntityCreated,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            // Named outright, the way the automation handlers do. A commit runs on the job server with
            // no request to take an identity from, so leaving it to be resolved records no actor at all.
            ActorUserId = request.UserId,
            Payload = new EntityCreatedPayload
            {
                Name = task.Name,
                StatusId = task.StatusId,
                StatusCategory = status.Category.ToString(),
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Project),
                    EntityId = context.Project.Id.ToString(),
                },
            ],
        }, cancellationToken);
    }

    private async Task Announce(IReadOnlyList<int> createdTaskIds, ImportApplyRequest request, CancellationToken cancellationToken)
    {
        if (createdTaskIds.Count == 0)
        {
            return;
        }

        Activity?.LogMany(options =>
        {
            options.EntityIds = createdTaskIds.ToList();
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Create;
            options.UserId = request.UserId;
            options.WorkspaceId = request.WorkspaceId;
        });

        foreach (var taskId in createdTaskIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await EventPublisher.Dispatch(new TaskCreatedMessage
            {
                WorkspaceId = request.WorkspaceId,
                TaskId = taskId,
                ActorUserId = request.UserId,
            });
        }
    }

    private async IAsyncEnumerable<ImportRow> ReadRows(
        ImportApplyRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = Readers.FirstOrDefault(candidate => candidate.Kinds.Contains(request.Session.SourceKind))
            ?? throw new NotSupportedException($"'{request.Session.SourceKind}' files cannot be read yet.");
        var skipHeader = request.ReadOptions.HasHeaderRow;
        var isFirst = true;

        await foreach (var row in reader.ReadRows(request.Source, request.ReadOptions, cancellationToken))
        {
            if (isFirst && skipHeader)
            {
                isFirst = false;
                continue;
            }

            isFirst = false;

            yield return row;
        }
    }

    private async Task<ImportContext> LoadContext(ImportApplyRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = request.WorkspaceId;
        var session = request.Session;

        await UnitOfWork.Statuses.EnsureNewTaskStatus(workspaceId, request.UserId, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var statuses = await UnitOfWork.Statuses.GetAllInWorkspace(workspaceId, cancellationToken: cancellationToken);
        var tags = await UnitOfWork.Tags.GetTagsInWorkspace(workspaceId, cancellationToken: cancellationToken);
        var board = session.TargetBoardIdentifier is null
            ? null
            : await UnitOfWork.Boards.GetByIdentifier(session.TargetBoardIdentifier, workspaceId, cancellationToken: cancellationToken);
        var project = board is null
            ? null
            : await UnitOfWork.Projects.GetAsync(board.ProjectId, cancellationToken: cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("The destination board could not be resolved.");
        }

        var groups = await UnitOfWork.BoardGroups.GetBoardGroupsInBoard(board!.Id, cancellationToken: cancellationToken);
        var members = await UnitOfWork.Users.GetWorkspaceUsers(request.WorkspaceSlug, true, cancellationToken);
        var sprints = await UnitOfWork.Sprints.GetAllInWorkspace(workspaceId, cancellationToken: cancellationToken);

        // Only read when the mapping actually matches against existing rows. Without a dedupe key every
        // row is a create, and pulling the workspace's whole task table in to answer nothing is the
        // single most expensive thing preview and commit do.
        var existing = request.Mapping.Dedupe is null
            ? []
            : await UnitOfWork.Tasks.GetAllInWorkspace(workspaceId, cancellationToken: cancellationToken);

        var vocabulary = new ImportVocabulary
        {
            StatusesByKey = statuses.Where(status => status.EntityType == EntityType.Task)
                .ToDictionary(status => status.Key.ToLowerInvariant(), status => status),
            StatusesByName = statuses.Where(status => status.EntityType == EntityType.Task)
                .GroupBy(status => status.Name.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First()),
            TagsByName = tags.GroupBy(tag => tag.Name.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First()),
            UsersByEmail = members.Where(user => user.Email is not null)
                .GroupBy(user => user.Email!.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First()),
            BoardGroupsByName = groups.GroupBy(group => group.Name.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First()),
            SprintsByName = sprints.GroupBy(sprint => sprint.Name.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First()),
        };

        return new ImportContext(project, board, vocabulary, statuses, groups, existing);
    }

    private static Status ResolveStatus(ResolvedTaskRow row, ImportContext context, ImportMappingModel mapping)
    {
        var matched = context.Vocabulary.FindStatus(row.StatusValue);

        if (matched is not null)
        {
            return matched;
        }

        var fallback = context.Vocabulary.FindStatus(mapping.Defaults.StatusKey);

        return fallback ?? context.DefaultStatus;
    }

    private static ImportRowAction ResolveAction(ResolvedTaskRow row, ImportApplyRequest request, ImportContext context)
    {
        if (row.HasErrors && !request.SkipFailingRows)
        {
            return ImportRowAction.Error;
        }

        if (row.HasErrors)
        {
            return ImportRowAction.Skip;
        }

        var dedupe = request.Mapping.Dedupe;

        if (dedupe is null)
        {
            return ImportRowAction.Create;
        }

        var existing = context.FindExisting(row);

        if (existing is null)
        {
            return ImportRowAction.Create;
        }

        return dedupe.Action switch
        {
            ImportDedupeAction.SkipExisting => ImportRowAction.Skip,
            ImportDedupeAction.UpdateExisting => ImportRowAction.Update,
            _ => ImportRowAction.Create,
        };
    }

    private static void CollectVocabularyGaps(
        ResolvedTaskRow row,
        ImportMappingModel mapping,
        ImportContext context,
        HashSet<string> newTags,
        HashSet<string> newGroups,
        HashSet<string> invites,
        List<ImportRowDiagnostic> diagnostics)
    {
        foreach (var tag in row.TagValues.Where(tag => context.Vocabulary.FindTag(tag) is null))
        {
            if (mapping.UnknownPolicy.Tags == ImportUnknownPolicy.Create)
            {
                newTags.Add(tag);
            }
        }

        var groupName = row.BoardGroupValue ?? mapping.Defaults.BoardGroupName;
        var groupIsMissing = groupName is not null && context.Vocabulary.FindBoardGroup(groupName) is null;

        if (groupIsMissing && mapping.UnknownPolicy.BoardGroups == ImportUnknownPolicy.Create)
        {
            newGroups.Add(groupName!);
        }

        foreach (var assignee in row.AssigneeValues.Where(assignee => context.Vocabulary.FindUser(assignee) is null))
        {
            invites.Add(assignee);

            diagnostics.Add(new ImportRowDiagnostic
            {
                RowNumber = row.RowNumber,
                Severity = ImportDiagnosticSeverity.Warning,
                Code = ImportDiagnosticCodes.UnresolvedUser,
                Message = $"'{assignee}' is not a member of this workspace and will not be assigned.",
                Value = assignee,
            });
        }
    }

    private static List<ImportNewEntity> BuildNewEntities(HashSet<string> newTags, HashSet<string> newGroups)
    {
        var tags = newTags.Select(name => new ImportNewEntity { EntityType = "tag", Name = name });
        var groups = newGroups.Select(name => new ImportNewEntity { EntityType = "board-group", Name = name });

        return tags.Concat(groups).ToList();
    }

    private static ImportRowPreview ToSample(ResolvedTaskRow row, ImportRowAction action)
    {
        return new ImportRowPreview
        {
            RowNumber = row.RowNumber,
            Action = action,
            Resolved = new Dictionary<string, string?>
            {
                ["name"] = row.Name,
                ["status"] = row.StatusValue,
                ["dueDate"] = row.DueDate?.ToString("yyyy-MM-dd"),
                ["assignees"] = string.Join(", ", row.AssigneeValues),
                ["tags"] = string.Join(", ", row.TagValues),
            },
        };
    }

    private sealed record PlannedRow(ResolvedTaskRow Row, ImportRowAction Action);

    private sealed record Creation(ResolvedTaskRow Row, ProjectTask Task, Status Status);

    private sealed record Update(ImportSessionEntry Entry, ProjectTask Task);

    private sealed class ActionCounts
    {
        public int Created;
        public int Updated;
        public int Skipped;
        public int Failed;

        public void Add(ImportRowAction action)
        {
            switch (action)
            {
                case ImportRowAction.Create:
                    Created++;
                    return;
                case ImportRowAction.Update:
                    Updated++;
                    return;
                case ImportRowAction.Skip:
                    Skipped++;
                    return;
                default:
                    Failed++;
                    return;
            }
        }
    }
}
