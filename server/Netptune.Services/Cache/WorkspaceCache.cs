using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Cache.Common;

namespace Netptune.Services.Cache;

public class WorkspaceCache : EntityCache<Workspace, string>, IWorkspaceCache
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public WorkspaceCache(
        ICacheProvider cache,
        INetptuneUnitOfWork unitOfWork,
        ILogger<WorkspaceCache> logger)
        : base(cache, TimeSpan.FromHours(1), logger)
    {
        UnitOfWork = unitOfWork;
    }

    protected override Task<Workspace> GetEntity(string key)
    {
        return UnitOfWork.Workspaces.GetBySlug(key);
    }

    protected override string GetCacheKey(string key)
    {
        return $"workspace:{key}";
    }

    public async Task<int?> GetIdBySlug(string slug)
    {
        var entity = await Get(slug);

        return entity?.Id;
    }
}