using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Transfer.ViewModels;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class ImportSessionRepository : WorkspaceEntityRepository<DataContext, ImportSession, int>, IImportSessionRepository
{
    public ImportSessionRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory)
    {
    }

    public Task<ImportSession?> GetByPublicId(Guid publicId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(session => session.PublicId == publicId && session.WorkspaceId == workspaceId && !session.IsDeleted);

        if (isReadonly)
        {
            return query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ImportSession?> GetForProcessing(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(session => session.Workspace)
            .SingleOrDefaultAsync(session => session.Id == id && !session.IsDeleted, cancellationToken);
    }

    public async Task<PagedResponse<ImportSessionViewModel>> GetSessions(int workspaceId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(session => session.WorkspaceId == workspaceId && !session.IsDeleted);
        var total = await query.CountAsync(cancellationToken);
        var pagination = page.GetPagination();
        var items = await Project(query)
            .OrderByDescending(session => session.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ImportSessionViewModel>(items, pagination.Page, pagination.PageSize, total);
    }

    public Task<ImportSessionViewModel?> GetViewModel(Guid publicId, int workspaceId, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(session => session.PublicId == publicId && session.WorkspaceId == workspaceId && !session.IsDeleted);

        return Project(query).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<ImportSessionEntry>> GetEntries(int sessionId, CancellationToken cancellationToken = default)
    {
        return Context.ImportSessionEntries
            .Where(entry => entry.SessionId == sessionId)
            .OrderByDescending(entry => entry.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddEntries(IEnumerable<ImportSessionEntry> entries, CancellationToken cancellationToken = default)
    {
        await Context.ImportSessionEntries.AddRangeAsync(entries, cancellationToken);
    }

    public Task<List<ImportSession>> GetExpired(DateTime expiredBefore, int take, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(session => session.ExpiresAt <= expiredBefore && !session.QuotaReleased && !session.IsDeleted)
            .OrderBy(session => session.ExpiresAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<ImportSessionViewModel> Project(IQueryable<ImportSession> query)
    {
        return query.Select(session => new ImportSessionViewModel
        {
            PublicId = session.PublicId,
            Stage = session.Stage,
            SourceKind = session.SourceKind,
            VendorProfile = session.VendorProfile,
            OriginalName = session.OriginalName,
            SizeBytes = session.SizeBytes,
            TargetRecordType = session.TargetRecordType,
            TargetProjectKey = session.TargetProjectKey,
            TargetBoardIdentifier = session.TargetBoardIdentifier,
            ProgressPercent = session.ProgressPercent,
            ProgressMessage = session.ProgressMessage,
            Error = session.Error,
            Created = session.Entries.Count(entry => entry.Operation == ImportEntryOperation.Created),
            Updated = session.Entries.Count(entry => entry.Operation == ImportEntryOperation.Updated),
            CanUndo = session.Stage == ImportStage.Committed && session.Entries.Any(),
            CreatedByUserId = session.CreatedBy,
            CreatedByDisplayName = session.CreatedByUser == null
                ? null
                : session.CreatedByUser.Firstname + " " + session.CreatedByUser.Lastname,
            CreatedAt = session.CreatedAt,
            CommittedAt = session.CommittedAt,
            ExpiresAt = session.ExpiresAt,
        });
    }
}
