using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiMessageEntityMap : KeyedEntityMap<AiMessage, long>
{
    public override void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        base.Configure(builder);

        builder
            .Property(message => message.Sequence)
            .IsRequired();

        builder
            .Property(message => message.Role)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(message => message.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(message => message.ProviderPayload)
            .HasColumnType("jsonb").IsRequired(false);

        builder
            .Property(message => message.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(message => message.Model)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(message => message.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(message => message.FinishReason)
            .HasMaxLength(64);

        builder
            .Property(message => message.Error)
            .HasColumnType("text");

        builder
            .Property(message => message.CreatedAt)
            .IsRequired();

        builder.HasIndex(message => new { message.ConversationId, message.Sequence })
            .IsUnique()
            .HasDatabaseName("ix_ai_messages_conversation_sequence");

        builder
            .HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
