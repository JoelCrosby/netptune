using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class ProjectEntityMap : AuditableEntityMap<Project, int>
    {
        public override void Configure(EntityTypeBuilder<Project> builder)
        {
            base.Configure(builder);

            builder
                .Property(project => project.Name)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(project => project.Description)
                .HasMaxLength(4096);

            builder
                .Property(project => project.RepositoryUrl)
                .HasMaxLength(1024);

            builder
                .HasOne(project => project.Workspace)
                .WithMany(workspace => workspace.Projects)
                .Metadata.DeleteBehavior = DeleteBehavior.Restrict;

            // (One-to-One) Project > Task

            builder
                .HasMany(project => project.ProjectTasks)
                .WithOne(task => task.Project);

            // (One-to-One) Project > Post

            builder
                .HasMany(project => project.ProjectPosts)
                .WithOne(post => post.Project)
                .HasForeignKey(post => post.ProjectId);
        }
    }
}
