using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class CommentEntityMap : WorkspaceEntityMap<Comment, int>
    {
        public override void Configure(EntityTypeBuilder<Comment> builder)
        {
            base.Configure(builder);

            builder
                .Property(comment => comment.Body)
                .HasMaxLength(32768)
                .IsRequired();

            builder
                .Property(comment => comment.EntityId)
                .IsRequired();

            builder
                .Property(comment => comment.EntityType)
                .IsRequired();

            builder
                .HasMany(comment => comment.Reactions)
                .WithOne(reaction => reaction.Comment)
                .HasForeignKey(reaction => reaction.CommentId)
                .IsRequired();
        }
    }
}
