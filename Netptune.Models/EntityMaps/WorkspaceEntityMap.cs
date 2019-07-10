using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class WorkspaceEntityMap : AuditableEntityMap<Workspace, int>
    {
        public override void Configure(EntityTypeBuilder<Workspace> builder)
        {
            base.Configure(builder);

            // (One-to-One) Workspace > Task

            builder
                .HasMany(worspace => worspace.ProjectTasks)
                .WithOne(task => task.Workspace)
                .HasForeignKey(task => task.WorkspaceId)
                .IsRequired();
        }
    }
}
