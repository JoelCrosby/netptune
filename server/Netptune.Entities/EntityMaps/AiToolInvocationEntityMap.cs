using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiToolInvocationEntityMap : KeyedEntityMap<AiToolInvocation, long>
{
    public override void Configure(EntityTypeBuilder<AiToolInvocation> builder)
    {
        base.Configure(builder);

        builder
            .Property(invocation => invocation.ToolName)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(invocation => invocation.Arguments)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(invocation => invocation.Result)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .Property(invocation => invocation.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(invocation => invocation.Error)
            .HasColumnType("text");
        builder
            .Property(invocation => invocation.CreatedAt)
            .IsRequired();

        builder
            .HasIndex(invocation => invocation.ConversationId);

        builder
            .HasOne(invocation => invocation.Message)
            .WithMany()
            .HasForeignKey(invocation => invocation.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
