using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AutomationActionResultEntityMap : AuditableEntityMap<AutomationActionResult, int>
{
    public override void Configure(EntityTypeBuilder<AutomationActionResult> builder)
    {
        base.Configure(builder);

        builder
            .Property(result => result.ActionType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(result => result.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(result => result.IdempotencyKey)
            .HasMaxLength(768)
            .IsRequired();

        builder
            .Property(result => result.Message)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder
            .Property(result => result.Output)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .HasOne(result => result.AutomationRun)
            .WithMany(run => run.ActionResults)
            .HasForeignKey(result => result.AutomationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(result => result.IdempotencyKey)
            .IsUnique();

        builder
            .HasIndex(result => new { result.AutomationRunId, result.SortOrder });
    }
}
