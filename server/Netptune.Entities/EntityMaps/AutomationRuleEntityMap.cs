using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class AutomationRuleEntityMap : WorkspaceEntityMap<AutomationRule, int>
{
    public override void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        base.Configure(builder);

        builder
            .Property(rule => rule.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(rule => rule.IsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder
            .Property(rule => rule.TriggerType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(rule => rule.TriggerConfig)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .HasIndex(rule => new { rule.WorkspaceId, rule.IsEnabled });
    }
}
