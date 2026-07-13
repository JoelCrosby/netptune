using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class RelationTypeEntityMap : WorkspaceEntityMap<RelationType, int>
{
    public override void Configure(EntityTypeBuilder<RelationType> builder)
    {
        base.Configure(builder);

        builder
            .Property(relationType => relationType.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(relationType => relationType.InverseName)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(relationType => relationType.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(relationType => relationType.Description)
            .HasMaxLength(512);

        builder
            .Property(relationType => relationType.Color)
            .HasMaxLength(32);

        builder
            .Property(relationType => relationType.Category)
            .HasConversion<int>()
            .IsRequired();

        builder
            .HasIndex(relationType => new { relationType.WorkspaceId, relationType.Key })
            .IsUnique();

        builder
            .HasIndex(relationType => new { relationType.WorkspaceId, relationType.IsDeleted, relationType.SortOrder, relationType.Id })
            .HasDatabaseName("ix_relation_types_workspace_deleted_sort_id");
    }
}
