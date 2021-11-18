using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class ProjectEntityMap : WorkspaceEntityMap<Project, int>
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
            .Property(project => project.Key)
            .HasMaxLength(6)
            .IsRequired();

        builder
            .HasOne(project => project.Workspace)
            .WithMany(workspace => workspace.Projects)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(project => new { project.WorkspaceId, project.Key })
            .IsUnique();

        builder
            .Property(project => project.MetaInfo)
            .HasColumnType("jsonb")
            .IsRequired();

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