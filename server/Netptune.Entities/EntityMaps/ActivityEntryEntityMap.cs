using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class ActivityEntryEntityMap : WorkspaceEntityMap<ActivityEntry, int>
{
    public const string OpenEntryIndexName = "ux_activity_entries_open";

    public const string OpenEntryIndexFilter = "is_open AND NOT is_deleted";

    public const string OpenEntryIndexFilterToken = "$open_entry_index_filter$";

    public override void Configure(EntityTypeBuilder<ActivityEntry> builder)
    {
        base.Configure(builder);

        builder
            .HasIndex(entry => new { entry.WorkspaceId, entry.EntityType, entry.EntityId, entry.UserId })
            .IsUnique()
            .HasFilter(OpenEntryIndexFilter)
            .HasDatabaseName(OpenEntryIndexName);

        // The sweeper's claim (close_expired_activity_entries.sql) filters on notified_at, not is_open: an
        // entry closed early by the handler to free the unique-index slot above is still owed its
        // notifications, and keying this index — or the claim — on is_open would hide it forever.
        builder
            .HasIndex(entry => entry.WindowExpiresAt)
            .HasFilter("notified_at IS NULL")
            .HasDatabaseName("ix_activity_entries_pending_window_expires");

        builder
            .HasIndex(entry => new { entry.EntityType, entry.EntityId, entry.LastOccurredAt, entry.Id })
            .IsDescending(false, false, true, true)
            .HasDatabaseName("ix_activity_entries_entity_last_occurred_id");

        builder
            .Property(entry => entry.EntityType)
            .IsRequired();

        builder
            .Property(entry => entry.EntityId)
            .IsRequired();

        builder
            .Property(entry => entry.ActivityType)
            .IsRequired();

        builder
            .Property(entry => entry.FirstOccurredAt)
            .IsRequired();

        builder
            .Property(entry => entry.LastOccurredAt)
            .IsRequired();

        builder
            .Property(entry => entry.RevisionCount)
            .HasDefaultValue(0);

        builder
            .Property(entry => entry.IsOpen)
            .HasDefaultValue(true);

        builder
            .Property(entry => entry.WindowExpiresAt)
            .IsRequired();

        builder
            .Property(entry => entry.NotifiedAt)
            .IsRequired(false);

        builder
            .HasOne(entry => entry.User)
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
