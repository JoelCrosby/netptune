using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiChangeSetEntityMap : WorkspaceEntityMap<AiChangeSet, Guid>
{
    public override void Configure(EntityTypeBuilder<AiChangeSet> builder)
    {
        base.Configure(builder);

        builder
            .Property(changeSet => changeSet.UserId)
            .IsRequired();

        builder
            .Property(changeSet => changeSet.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(changeSet => changeSet.CorrelationId)
            .IsRequired();

        builder
            .HasIndex(changeSet => new { changeSet.ConversationId, changeSet.Status });

        builder
            .HasOne(changeSet => changeSet.Conversation)
            .WithMany()
            .HasForeignKey(changeSet => changeSet.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AiProposedChangeEntityMap : KeyedEntityMap<AiProposedChange, long>
{
    public override void Configure(EntityTypeBuilder<AiProposedChange> builder)
    {
        base.Configure(builder);

        builder
            .Property(change => change.Sequence)
            .IsRequired();

        builder
            .Property(change => change.ToolName)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(change => change.EntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(change => change.RefKey)
            .HasMaxLength(32);

        builder
            .Property(change => change.Summary)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(change => change.Fields)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(change => change.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(change => change.ValidationStatus)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(change => change.ValidationMessage)
            .HasColumnType("text");

        builder
            .Property(change => change.ApplyStatus)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(change => change.ApplyError)
            .HasColumnType("text");

        builder
            .HasIndex(change => new { change.ChangeSetId, change.Sequence })
            .IsUnique()
            .HasDatabaseName("ix_ai_proposed_changes_change_set_sequence");

        builder
            .HasOne(change => change.ChangeSet)
            .WithMany(changeSet => changeSet.Changes)
            .HasForeignKey(change => change.ChangeSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
