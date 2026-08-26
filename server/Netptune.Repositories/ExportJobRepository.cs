using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Repositories;

public sealed class ExportJobRepository : WorkspaceEntityRepository<DataContext, ExportJob, int>, IExportJobRepository
{
    public ExportJobRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public Task<ExportJob?> GetByPublicId(Guid publicId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(job => job.PublicId == publicId && job.WorkspaceId == workspaceId && !job.IsDeleted);

        if (isReadonly)
        {
            return query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ExportJob?> GetForProcessing(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(job => job.Workspace)
            .SingleOrDefaultAsync(job => job.Id == id && !job.IsDeleted, cancellationToken);
    }

    public async Task<ExportJobStatus?> GetStatus(int id, CancellationToken cancellationToken = default)
    {
        var statuses = await Entities
            .AsNoTracking()
            .Where(job => job.Id == id)
            .Select(job => job.Status)
            .ToListAsync(cancellationToken);

        return statuses.Count == 0 ? null : statuses[0];
    }

    public async Task<PagedResponse<ExportJobViewModel>> GetExportJobs(int workspaceId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(job => job.WorkspaceId == workspaceId && !job.IsDeleted);
        var total = await query.CountAsync(cancellationToken);
        var pagination = page.GetPagination();
        var items = await Project(query)
            .OrderByDescending(job => job.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ExportJobViewModel>(items, pagination.Page, pagination.PageSize, total);
    }

    public Task<ExportJobViewModel?> GetViewModel(Guid publicId, int workspaceId, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(job => job.PublicId == publicId && job.WorkspaceId == workspaceId && !job.IsDeleted);

        return Project(query).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountUnfinished(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities.CountAsync(
            job => job.WorkspaceId == workspaceId &&
                !job.IsDeleted &&
                (job.Status == ExportJobStatus.Pending || job.Status == ExportJobStatus.Running),
            cancellationToken);
    }

    public Task<List<ExportJob>> GetExpired(DateTime expiredBefore, int take, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(job => job.ExpiresAt <= expiredBefore && job.Status != ExportJobStatus.Expired && !job.IsDeleted)
            .OrderBy(job => job.ExpiresAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ExportJob>> GetStaleRunning(DateTime startedBefore, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(job => job.Status == ExportJobStatus.Running && job.StartedAt != null && job.StartedAt < startedBefore && !job.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public Task<long> GetExpectedStorageUsage(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(job => job.WorkspaceId == workspaceId && !job.IsDeleted && !job.QuotaReleased && job.SizeBytes != null)
            .SumAsync(job => job.SizeBytes!.Value, cancellationToken);
    }

    private static IQueryable<ExportJobViewModel> Project(IQueryable<ExportJob> query)
    {
        return query.Select(job => new ExportJobViewModel
        {
            PublicId = job.PublicId,
            Status = job.Status,
            RecordType = job.RecordType,
            Format = job.Format,
            Name = job.Name,
            FileName = job.FileName,
            RowCount = job.RowCount,
            SizeBytes = job.SizeBytes,
            ProgressPercent = job.ProgressPercent,
            ProgressMessage = job.ProgressMessage,
            Error = job.Error,
            HasArtefact = job.Status == ExportJobStatus.Succeeded && job.StorageKey != null,
            RequestedByUserId = job.RequestedBy,
            RequestedByDisplayName = job.CreatedByUser == null
                ? null
                : job.CreatedByUser.Firstname + " " + job.CreatedByUser.Lastname,
            RequestedByPictureUrl = job.CreatedByUser == null ? null : job.CreatedByUser.PictureUrl,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ExpiresAt = job.ExpiresAt,
        });
    }
}
