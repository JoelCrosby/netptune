using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class ReactionEntityMap : WorkspaceEntityMap<Reaction, int>
    {
        public override void Configure(EntityTypeBuilder<Reaction> builder)
        {
            base.Configure(builder);

            builder
                .Property(reaction => reaction.Value)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(reaction => reaction.CommentId)
                .IsRequired();

            builder
                .HasOne(reaction => reaction.Comment)
                .WithMany(comment => comment.Reactions)
                .HasForeignKey(reaction => reaction.CommentId)
                .IsRequired();
        }
    }
}
