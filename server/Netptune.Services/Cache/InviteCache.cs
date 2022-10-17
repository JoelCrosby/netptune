using System;
using System.Threading.Tasks;

using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Models.Authentication;
using Netptune.Services.Cache.Common;

namespace Netptune.Services.Cache;

public class InviteCache : ValueCache<WorkspaceInvite>, IInviteCache
{
    public InviteCache(ICacheProvider cache) : base(cache, TimeSpan.FromDays(7))
    {
    }

    public override Task<WorkspaceInvite?> Get(string? key)
    {
        return base.Get($"workspace-invite:{key}");
    }

    public override Task Create(string? key, WorkspaceInvite value)
    {
        return base.Create($"workspace-invite:{key}", value);
    }

    public override void Remove(string? key)
    {
        base.Remove($"workspace-invite:{key}");
    }
}
