using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiConversationEntityMap : WorkspaceEntityMap<AiConversation, Guid>
{
    public override void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        base.Configure(builder);

        builder
            .Property(conversation => conversation.UserId)
            .IsRequired();

        builder
            .Property(conversation => conversation.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(conversation => conversation.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(conversation => conversation.Model)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(conversation => conversation.RequestedEffort)
            .HasConversion<int?>();

        builder
            .Property(conversation => conversation.LastMessageAt)
            .IsRequired();

        builder.HasIndex(conversation => new
        {
            conversation.WorkspaceId,
            conversation.UserId,
            conversation.IsDeleted,
            conversation.LastMessageAt,
        })
            .HasDatabaseName("ix_ai_conversations_workspace_user_last_message");

        builder
            .HasOne(conversation => conversation.User)
            .WithMany()
            .HasForeignKey(conversation => conversation.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
