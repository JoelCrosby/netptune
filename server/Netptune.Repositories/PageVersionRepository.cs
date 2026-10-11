using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Pages;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class PageVersionRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, PageVersion, int>(context, connectionFactory), IPageVersionRepository
{
    public Task<PageVersion?> GetLatest(int pageId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(version => version.PageId == pageId)
            .OrderByDescending(version => version.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<PageVersionSummaryViewModel>> GetSummaries(int pageId, int workspaceId, CancellationToken cancellationToken = default)
    {
        var versions = Entities.AsNoTracking();

        return Entities
            .AsNoTracking()
            .Where(version => version.PageId == pageId && version.Page!.WorkspaceId == workspaceId && !version.Page.IsDeleted)
            .OrderByDescending(version => version.Id)
            .Take(PaginationDefaults.MaxUnpagedRows)
            .Select(version => new PageVersionSummaryViewModel
            {
                Id = version.Id,
                Revision = version.Revision,
                Title = version.Title,
                CreatedAt = version.CreatedAt,
                SavedAt = version.UpdatedAt ?? version.CreatedAt,
                CreatedByDisplayName = version.CreatedByUser == null ? null : version.CreatedByUser.DisplayName,
                RestoredFromVersionId = version.RestoredFromVersionId,
                RestoredFromCreatedAt = versions
                    .Where(source => source.Id == version.RestoredFromVersionId)
                    .Select(source => (DateTime?)source.CreatedAt)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<PageVersionViewModel?> GetViewModel(int versionId, int pageId, int workspaceId, CancellationToken cancellationToken = default)
    {
        var versions = Entities.AsNoTracking();

        return Entities
            .AsNoTracking()
            .Where(version => version.Id == versionId && version.PageId == pageId)
            .Where(version => version.Page!.WorkspaceId == workspaceId && !version.Page.IsDeleted)
            .Select(version => new PageVersionViewModel
            {
                Id = version.Id,
                PageId = version.PageId,
                Revision = version.Revision,
                Title = version.Title,
                Body = version.Body,
                PreviousBody = versions
                    .Where(previous => previous.PageId == version.PageId && previous.Id < version.Id)
                    .OrderByDescending(previous => previous.Id)
                    .Select(previous => previous.Body)
                    .FirstOrDefault(),
                CreatedAt = version.CreatedAt,
                SavedAt = version.UpdatedAt ?? version.CreatedAt,
                CreatedByDisplayName = version.CreatedByUser == null ? null : version.CreatedByUser.DisplayName,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
