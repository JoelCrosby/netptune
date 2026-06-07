using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class AutomationActionEntityMap : AuditableEntityMap<AutomationAction, int>
{
    public override void Configure(EntityTypeBuilder<AutomationAction> builder)
    {
        base.Configure(builder);

        builder
            .Property(action => action.Type)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(action => action.SortOrder)
            .IsRequired();

        builder
            .Property(action => action.Config)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .HasOne(action => action.AutomationRule)
            .WithMany(rule => rule.Actions)
            .HasForeignKey(action => action.AutomationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(action => new { action.AutomationRuleId, action.SortOrder });
    }
}
