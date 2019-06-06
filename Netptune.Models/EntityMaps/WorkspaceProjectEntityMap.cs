using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;


namespace Netptune.Entities.EntityMaps
{
    public class WorkspaceProjectEntityMap : KeyedEntityMap<WorkspaceProject, int>
    {

        public override void Configure(EntityTypeBuilder<WorkspaceProject> builder)
        {
            base.Configure(builder);

            // (Many-to-many) Workspace > Project

            builder
                .HasKey(pt => new { pt.WorkspaceId, pt.ProjectId });

            builder
                .HasOne(pt => pt.Workspace)
                .WithMany(p => p.WorkspaceProjects)
                .HasForeignKey(pt => pt.WorkspaceId);

            builder
                .HasOne(pt => pt.Project)
                .WithMany(t => t.WorkspaceProjects)
                .HasForeignKey(pt => pt.ProjectId);
        }

    }
}
