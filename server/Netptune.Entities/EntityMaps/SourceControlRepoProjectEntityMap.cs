using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class SourceControlRepoProjectEntityMap : KeyedEntityMap<SourceControlRepoProject, int>
{
    public override void Configure(EntityTypeBuilder<SourceControlRepoProject> builder)
    {
        base.Configure(builder);

        builder
            .HasOne(link => link.Repo)
            .WithMany(repo => repo.Projects)
            .HasForeignKey(link => link.RepoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(link => link.Project)
            .WithMany()
            .HasForeignKey(link => link.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(link => new { link.RepoId, link.ProjectId })
            .IsUnique()
            .HasDatabaseName("ix_source_control_repo_projects_repo_project");

        builder
            .HasIndex(link => link.ProjectId)
            .HasDatabaseName("ix_source_control_repo_projects_project");
    }
}
