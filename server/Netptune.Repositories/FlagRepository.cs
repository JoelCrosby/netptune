using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class FlagRepository : WorkspaceEntityRepository<DataContext, Flag, int>, IFlagRepository
{
    public FlagRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<Flag>> GetExistingAutomationTaskFlags(
        IReadOnlyCollection<int> ruleIds,
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(flag =>
                !flag.IsDeleted &&
                flag.AutomationRuleId.HasValue &&
                flag.EntityType == EntityType.Task &&
                flag.EntityId.HasValue)
            .Where(flag => flag.AutomationRuleId.HasValue && ruleIds.Contains(flag.AutomationRuleId.Value))
            .Where(flag => flag.EntityId.HasValue && taskIds.Contains(flag.EntityId.Value))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
