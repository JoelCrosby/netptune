using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;

namespace Netptune.Entities.EntityMaps;

public sealed class EventRecordEntityMap : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        builder.ToTable("event_records");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedOnAdd();
        builder.HasIndex(record => record.EventId).IsUnique();

        builder.Property(record => record.EventKey).HasMaxLength(128).IsRequired();
        builder.Property(record => record.SchemaVersion).IsRequired();
        builder.Property(record => record.SubjectType).HasMaxLength(64);
        builder.Property(record => record.SubjectId).HasMaxLength(128);
        builder.Property(record => record.UserAgent).HasMaxLength(1024);
        builder.Property(record => record.RetentionClass).HasMaxLength(32).IsRequired();
        builder.Property(record => record.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(record => record.IpAddress).HasColumnType("inet");

        builder.HasIndex(record => new { record.WorkspaceId, record.OccurredAt, record.Id })
            .HasDatabaseName("ix_event_records_workspace_occurred_id");
        builder.HasIndex(record => new
        {
            record.WorkspaceId,
            record.SubjectType,
            record.SubjectId,
            record.SubjectSequence,
        })
            .IsUnique()
            .HasDatabaseName("ix_event_records_subject_sequence");
        builder.HasIndex(record => new { record.WorkspaceId, record.EventKey, record.OccurredAt, record.Id })
            .HasDatabaseName("ix_event_records_workspace_key_occurred_id");
    }
}

public sealed class EventReferenceEntityMap : IEntityTypeConfiguration<EventReference>
{
    public void Configure(EntityTypeBuilder<EventReference> builder)
    {
        builder.ToTable("event_references");
        builder.HasKey(reference => new
        {
            reference.EventRecordId,
            reference.Role,
            reference.EntityType,
            reference.EntityId,
        });

        builder.Property(reference => reference.Role).HasMaxLength(32);
        builder.Property(reference => reference.EntityType).HasMaxLength(64);
        builder.Property(reference => reference.EntityId).HasMaxLength(128);
        builder.HasIndex(reference => new
        {
            reference.EntityType,
            reference.EntityId,
            reference.Role,
            reference.EventRecordId,
        })
            .HasDatabaseName("ix_event_references_reverse_lookup");
        builder.HasOne(reference => reference.EventRecord)
            .WithMany(record => record.References)
            .HasForeignKey(reference => reference.EventRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EventStreamHeadEntityMap : IEntityTypeConfiguration<EventStreamHead>
{
    public void Configure(EntityTypeBuilder<EventStreamHead> builder)
    {
        builder.ToTable("event_stream_heads");
        builder.HasKey(head => new { head.WorkspaceId, head.SubjectType, head.SubjectId });
        builder.Property(head => head.SubjectType).HasMaxLength(64);
        builder.Property(head => head.SubjectId).HasMaxLength(128);
    }
}

public sealed class EventOutboxEntityMap : IEntityTypeConfiguration<EventOutbox>
{
    public void Configure(EntityTypeBuilder<EventOutbox> builder)
    {
        builder.ToTable("event_outbox");
        builder.HasKey(outbox => outbox.EventRecordId);
        builder.Property(outbox => outbox.LastError).HasColumnType("text");
        builder.HasIndex(outbox => outbox.DeadLetteredAt);
        builder.HasIndex(outbox => new { outbox.AvailableAt, outbox.LeaseExpiresAt });
        builder.HasOne(outbox => outbox.EventRecord)
            .WithOne(record => record.Outbox)
            .HasForeignKey<EventOutbox>(outbox => outbox.EventRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
