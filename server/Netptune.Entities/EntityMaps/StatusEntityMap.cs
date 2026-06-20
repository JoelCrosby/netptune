using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class StatusEntityMap : WorkspaceEntityMap<Status, int>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder
            .Property(status => status.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(status => status.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(status => status.Description)
            .HasMaxLength(512);

        builder
            .Property(status => status.Color)
            .HasMaxLength(32);

        builder
            .Property(status => status.EntityType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(status => status.Category)
            .HasConversion<int>()
            .IsRequired();

        builder
            .HasIndex(status => new { status.WorkspaceId, status.EntityType, status.Key })
            .IsUnique();

        builder
            .HasIndex(status => new { status.WorkspaceId, status.EntityType, status.IsDeleted, status.SortOrder, status.Id })
            .HasDatabaseName("ix_statuses_workspace_entity_deleted_sort_id");
    }
}
