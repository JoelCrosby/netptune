using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiUsageRecordEntityMap : KeyedEntityMap<AiUsageRecord, long>
{
    public override void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        base.Configure(builder);

        builder
            .Property(record => record.UserId)
            .IsRequired();

        builder
            .Property(record => record.Model)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(record => record.Purpose)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(record => record.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(record => record.Workspace)
            .WithMany()
            .HasForeignKey(record => record.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, as for conversations, so a departing member's spend stays on the workspace's books.
        builder
            .HasOne(record => record.User)
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(record => new
            {
                record.WorkspaceId,
                record.CreatedAt
            })
            .HasDatabaseName("ix_ai_usage_records_workspace_created");
    }
}
