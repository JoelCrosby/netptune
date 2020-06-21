using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class PostEntityMap : AuditableEntityMap<Post, int>
    {
        public override void Configure(EntityTypeBuilder<Post> builder)
        {
            base.Configure(builder);

            builder
                .Property(post => post.Title)
                .HasMaxLength(4096)
                .IsRequired();

            builder
                .Property(task => task.Body)
                .HasMaxLength(int.MaxValue);

            builder
                .Property(task => task.Type)
                .HasDefaultValue(PostType.Board)
                .IsRequired();
        }
    }
}
