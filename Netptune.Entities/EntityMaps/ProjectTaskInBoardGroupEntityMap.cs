using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;
using Netptune.Models.Relationships;

namespace Netptune.Entities.EntityMaps
{
    public class ProjectTaskInBoardGroupEntityMap : AuditableEntityMap<ProjectTaskInBoardGroup, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectTaskInBoardGroup> builder)
        {
            base.Configure(builder);

            builder
                .HasAlternateKey(taskInGroup => new
                {
                    taskInGroup.BoardGroupId,
                    taskInGroup.ProjectTaskId
                });
        }
    }
}
