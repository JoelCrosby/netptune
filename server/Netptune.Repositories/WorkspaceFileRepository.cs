using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Files;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace Netptune.Repositories;

public sealed class WorkspaceFileRepository : WorkspaceEntityRepository<DataContext, WorkspaceFile, int>, IWorkspaceFileRepository
{
    public WorkspaceFileRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public async Task<PagedResponse<WorkspaceFileViewModel>> GetWorkspaceFiles(int workspaceId, string currentUserId, bool canDeleteAny, WorkspaceFileFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(file => file.WorkspaceId == workspaceId &&
            !file.IsDeleted && file.Status == WorkspaceFileStatus.Ready);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            query = query.Where(file => EF.Functions.ILike(file.OriginalName, $"%{filter.Query}%"));
        }

        if (filter.Purpose is { } purpose)
        {
            query = query.Where(file => file.Purpose == purpose);
        }

        if (!string.IsNullOrWhiteSpace(filter.UploadedByUserId))
        {
            query = query.Where(file => file.CreatedByUserId == filter.UploadedByUserId);
        }

        if (filter.CreatedFrom is { } from)
        {
            query = query.Where(file => file.CreatedAt >= from);
        }

        if (filter.CreatedTo is { } to)
        {
            query = query.Where(file => file.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.TaskSystemId))
        {
            var taskId = await ResolveTaskId(workspaceId, filter.TaskSystemId, cancellationToken);
            query = taskId is null
                ? query.Where(_ => false)
                : query.Where(file => file.TaskFiles.Any(link => link.ProjectTaskId == taskId));
        }

        query = ApplyContentTypeFilter(query, filter.ContentTypeGroup);

        var total = await query.CountAsync(cancellationToken);
        var descending = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        query = filter.SortBy switch
        {
            "name" => descending
                ? query.OrderByDescending(file => file.OriginalName)
                : query.OrderBy(file => file.OriginalName),
            "sizeBytes" => descending
                ? query.OrderByDescending(file => file.SizeBytes)
                : query.OrderBy(file => file.SizeBytes),
            _ => descending
                ? query.OrderByDescending(file => file.CreatedAt)
                : query.OrderBy(file => file.CreatedAt),
        };

        var page = filter.GetPage();
        var pageSize = filter.GetPageSize();
        var items = await Project(query, currentUserId, canDeleteAny)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<WorkspaceFileViewModel>> GetTaskFiles(int workspaceId, int taskId, string currentUserId, bool canDeleteAny, CancellationToken cancellationToken = default)
    {
        var query = Entities
        .Where(file =>
            file.WorkspaceId == workspaceId &&
            file.IsDeleted == false &&
            file.Status == WorkspaceFileStatus.Ready &&
            file.TaskFiles.Any(link => link.ProjectTaskId == taskId));

        return await Project(query, currentUserId, canDeleteAny)
            .OrderByDescending(file => file.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkspaceFile?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .IsReadonly(isReadonly)
            .SingleOrDefaultAsync(file =>
                file.Id == id &&
                file.WorkspaceId == workspaceId &&
                file.IsDeleted == false, cancellationToken);
    }

    public Task<WorkspaceFile?> GetByContentId(string contentId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .IsReadonly(isReadonly)
            .SingleOrDefaultAsync(file =>
                file.ContentId == contentId &&
                file.WorkspaceId == workspaceId &&
                file.IsDeleted == false, cancellationToken);
    }

    public Task<WorkspaceFileViewModel?> GetViewModel(int id, string currentUserId, bool canDeleteAny, CancellationToken cancellationToken = default)
    {
        return Project(Entities.Where(file => file.Id == id), currentUserId, canDeleteAny)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryMarkQuotaReleased(int id, string userId, CancellationToken cancellationToken = default)
    {
        var updated = await Entities
            .Where(file => file.Id == id && !file.QuotaReleased)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(file => file.QuotaReleased, true)
                .SetProperty(file => file.Status, WorkspaceFileStatus.Failed)
                .SetProperty(file => file.IsDeleted, true)
                .SetProperty(file => file.DeletedByUserId, userId), cancellationToken);

        return updated > 0;
    }

    public Task<List<WorkspaceFile>> GetStalePending(DateTime createdBefore, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(file => file.Status == WorkspaceFileStatus.Pending && !file.IsDeleted && file.CreatedAt < createdBefore)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkReady(int id, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(file => file.Id == id && file.Status == WorkspaceFileStatus.Pending && !file.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(file => file.Status, WorkspaceFileStatus.Ready), cancellationToken);
    }

    public Task<List<string>> GetTombstoneStorageKeys(CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(file => file.IsDeleted || file.Status == WorkspaceFileStatus.Failed)
            .Select(file => file.StorageKey)
            .ToListAsync(cancellationToken);
    }

    public Task<long> GetExpectedStorageUsage(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(file => file.WorkspaceId == workspaceId && !file.IsDeleted && !file.QuotaReleased &&
                (file.Status == WorkspaceFileStatus.Ready || file.Status == WorkspaceFileStatus.Pending))
            .SumAsync(file => file.SizeBytes, cancellationToken);
    }

    private Task<int?> ResolveTaskId(int workspaceId, string systemId, CancellationToken cancellationToken)
    {
        var parts = systemId.Split('-');

        if (!int.TryParse(parts.LastOrDefault(), out var scopeId))
        {
            return Task.FromResult<int?>(null);
        }

        var projectKey = parts.Length > 1 ? parts[0] : null;

        return Context.ProjectTasks
            .Where(task => task.WorkspaceId == workspaceId && !task.IsDeleted &&
                task.ProjectScopeId == scopeId &&
                (projectKey == null || task.Project!.Key == projectKey))
            .Select(task => (int?)task.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<WorkspaceFile> ApplyContentTypeFilter(IQueryable<WorkspaceFile> query, string? contentTypeGroup)
    {
        return contentTypeGroup switch
        {
            "image" => query.Where(file => file.ContentType.StartsWith("image/")),
            "document" => query.Where(file => file.ContentType == "application/pdf" ||
                file.ContentType.StartsWith("text/")),
            "archive" => query.Where(file => file.ContentType.Contains("zip") ||
                file.ContentType.Contains("compressed")),
            "other" => query.Where(file => !file.ContentType.StartsWith("image/") &&
                file.ContentType != "application/pdf" && !file.ContentType.StartsWith("text/") &&
                !file.ContentType.Contains("zip") && !file.ContentType.Contains("compressed")),
            _ => query,
        };
    }

    private static IQueryable<WorkspaceFileViewModel> Project(IQueryable<WorkspaceFile> query, string currentUserId, bool canDeleteAny)
    {
        return query.Select(file => new WorkspaceFileViewModel
        {
            Id = file.Id,
            OriginalName = file.OriginalName,
            ContentType = file.ContentType,
            ContentTypeGroup = file.ContentType.StartsWith("image/") ? "image" :
                file.ContentType == "application/pdf" || file.ContentType.StartsWith("text/") ? "document" :
                file.ContentType.Contains("zip") || file.ContentType.Contains("compressed") ? "archive" : "other",
            SizeBytes = file.SizeBytes,
            Purpose = file.Purpose,
            CreatedAt = file.CreatedAt,
            UploadedByUserId = file.CreatedByUserId,
            UploadedByDisplayName = file.CreatedByUser == null
                ? null
                : file.CreatedByUser.Firstname + " " + file.CreatedByUser.Lastname,
            UploadedByPictureUrl = file.CreatedByUser == null ? null : file.CreatedByUser.PictureUrl,
            UploadedByIsServiceAccount = file.CreatedByUser != null &&
                file.CreatedByUser.UserType == AppUserType.ServiceAccount,
            TaskId = file.TaskFiles.Select(link => (int?)link.ProjectTask.Id).FirstOrDefault(),
            TaskSystemId = file.TaskFiles.Select(link => link.ProjectTask.Project == null
                ? link.ProjectTask.ProjectScopeId.ToString()
                : link.ProjectTask.Project.Key + "-" + link.ProjectTask.ProjectScopeId).FirstOrDefault(),
            TaskName = file.TaskFiles.Select(link => link.ProjectTask.Name).FirstOrDefault(),
            ContentUrl = "/api/workspaces/" + file.Workspace.Slug + "/files/" + file.ContentId + "/content",
            CanDelete = canDeleteAny || file.CreatedByUserId == currentUserId,
        });
    }
}
