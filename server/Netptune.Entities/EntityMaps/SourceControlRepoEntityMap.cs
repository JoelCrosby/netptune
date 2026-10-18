using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class SourceControlRepoEntityMap : KeyedEntityMap<SourceControlRepo, int>
{
    public override void Configure(EntityTypeBuilder<SourceControlRepo> builder)
    {
        base.Configure(builder);

        builder
            .Property(repo => repo.ExternalId)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(repo => repo.Owner)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(repo => repo.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(repo => repo.FullName)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(repo => repo.DefaultBranch)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(repo => repo.WebUrl)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .Property(repo => repo.IsAccessible)
            .HasDefaultValue(true);

        builder
            .Property(repo => repo.HistorySyncError)
            .HasMaxLength(512);

        builder
            .HasOne(repo => repo.Connection)
            .WithMany(connection => connection.Repos)
            .HasForeignKey(repo => repo.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(repo => repo.Workspace)
            .WithMany()
            .HasForeignKey(repo => repo.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(repo => new { repo.ConnectionId, repo.ExternalId })
            .IsUnique()
            .HasDatabaseName("ix_source_control_repos_connection_external");

        builder
            .HasIndex(repo => repo.WorkspaceId)
            .HasFilter("is_linked")
            .HasDatabaseName("ix_source_control_repos_workspace_linked");
    }
}
