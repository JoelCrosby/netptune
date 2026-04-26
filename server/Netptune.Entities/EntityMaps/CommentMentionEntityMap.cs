using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class CommentMentionEntityMap : WorkspaceEntityMap<CommentMention, int>
{
    public override void Configure(EntityTypeBuilder<CommentMention> builder)
    {
        base.Configure(builder);

        builder
            .HasIndex(m => new { m.CommentId, m.UserId })
            .IsUnique();

        builder
            .HasOne(m => m.Comment)
            .WithMany(c => c.Mentions)
            .HasForeignKey(m => m.CommentId)
            .IsRequired();

        builder
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .IsRequired();
    }
}
