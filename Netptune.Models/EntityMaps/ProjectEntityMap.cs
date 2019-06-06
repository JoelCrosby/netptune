using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class ProjectEntityMap : AuditableEntityMap<Project, int>
    {

        public override void Configure(EntityTypeBuilder<Project> builder)
        {
            base.Configure(builder);

            builder
                .HasOne(e => e.Workspace)
                .WithMany(c => c.Projects)
                .Metadata.DeleteBehavior = DeleteBehavior.Restrict;

            // (One-to-One) Project > Task

            builder
                .HasMany(c => c.ProjectTasks)
                .WithOne(e => e.Project);


            // (One-to-One) Project > Post

            builder
                .HasMany(c => c.ProjectPosts)
                .WithOne(e => e.Project)
                .HasForeignKey(P => P.ProjectId);
        }

    }
}
