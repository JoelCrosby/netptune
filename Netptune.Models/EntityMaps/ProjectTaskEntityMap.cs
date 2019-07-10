using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class ProjectTaskEntityMap : AuditableEntityMap<ProjectTask, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectTask> builder)
        {
            base.Configure(builder);
        }
    }
}
