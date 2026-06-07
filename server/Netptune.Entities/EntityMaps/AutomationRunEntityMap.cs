using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class AutomationRunEntityMap : AuditableEntityMap<AutomationRun, int>
{
    public override void Configure(EntityTypeBuilder<AutomationRun> builder)
    {
        base.Configure(builder);

        builder
            .Property(run => run.TriggerType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(run => run.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(run => run.EntityType)
            .HasConversion<int?>()
            .IsRequired(false);

        builder
            .Property(run => run.IdempotencyKey)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(run => run.Message)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder
            .Property(run => run.Context)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .HasOne(run => run.AutomationRule)
            .WithMany(rule => rule.Runs)
            .HasForeignKey(run => run.AutomationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(run => run.IdempotencyKey)
            .IsUnique();

        builder
            .HasIndex(run => new { run.AutomationRuleId, run.CreatedAt });
    }
}
