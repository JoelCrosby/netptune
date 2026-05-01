using Microsoft.EntityFrameworkCore;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class ProjectTaskTagRepository : Repository<DataContext, ProjectTaskTag, int>, IProjectTaskTagRepository
{
    public ProjectTaskTagRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<List<int>> DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();
        var ids = await Entities
            .Where(entity => taskIdList.Contains(entity.ProjectTaskId))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        await DeletePermanent(ids, cancellationToken);

        return ids;
    }
}