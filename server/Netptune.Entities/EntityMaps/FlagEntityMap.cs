using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class FlagEntityMap : WorkspaceEntityMap<Flag, int>
{
    public override void Configure(EntityTypeBuilder<Flag> builder)
    {
        base.Configure(builder);

        builder
            .Property(flag => flag.Name)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .Property(flag => flag.Description)
            .HasMaxLength(int.MaxValue);

        builder
            .Property(flag => flag.EntityType)
            .HasConversion<int?>()
            .IsRequired(false);

        builder
            .Property(flag => flag.EntityId)
            .IsRequired(false);

        builder
            .Property(flag => flag.AutomationRuleId)
            .IsRequired(false);

        builder
            .HasIndex(flag => new { flag.WorkspaceId, flag.EntityType, flag.EntityId });

        builder
            .HasIndex(flag => new { flag.AutomationRuleId, flag.EntityType, flag.EntityId })
            .IsUnique();
    }
}
