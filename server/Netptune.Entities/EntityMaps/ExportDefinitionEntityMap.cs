using Netptune.Transfer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ExportDefinitionEntityMap : WorkspaceEntityMap<ExportDefinition, int>
{
    public override void Configure(EntityTypeBuilder<ExportDefinition> builder)
    {
        base.Configure(builder);

        builder
            .Property(definition => definition.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(definition => definition.Description)
            .HasMaxLength(1024);

        builder
            .Property(definition => definition.RecordType)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(definition => definition.Format)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(definition => definition.Definition)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(definition => definition.IsShared)
            .HasDefaultValue(false);

        builder
            .HasIndex(definition => new
            {
                definition.WorkspaceId,
                definition.Name
            });
    }
}
