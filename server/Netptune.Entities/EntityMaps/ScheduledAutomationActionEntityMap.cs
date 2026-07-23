using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ScheduledAutomationActionEntityMap : AuditableEntityMap<ScheduledAutomationAction, int>
{
    public override void Configure(EntityTypeBuilder<ScheduledAutomationAction> builder)
    {
        base.Configure(builder);

        builder
            .Property(action => action.ActionType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(action => action.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(action => action.ExpectedStatusId)
            .IsRequired();

        builder
            .Property(action => action.ExecuteAt)
            .IsRequired();

        builder
            .Property(action => action.ProcessedAt)
            .IsRequired(false);

        builder
            .Property(action => action.ClaimId)
            .IsRequired(false);

        builder
            .Property(action => action.LeaseExpiresAt)
            .IsRequired(false);

        builder
            .Property(action => action.LastError)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder
            .Property(action => action.TriggerContext)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .Property(action => action.IdempotencyKey)
            .HasMaxLength(768)
            .IsRequired();

        builder.HasOne(action => action.AutomationRule)
            .WithMany()
            .HasForeignKey(action => action.AutomationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(action => action.AutomationAction)
            .WithMany()
            .HasForeignKey(action => action.AutomationActionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(action => action.Task)
            .WithMany()
            .HasForeignKey(action => action.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(action => action.IdempotencyKey)
            .IsUnique();

        builder
            .HasIndex(action => new { action.Status, action.ExecuteAt });

        builder
            .HasIndex(action => new { action.Status, action.LeaseExpiresAt });

        builder
            .HasIndex(action => new { action.TaskId, action.Status });
    }
}
