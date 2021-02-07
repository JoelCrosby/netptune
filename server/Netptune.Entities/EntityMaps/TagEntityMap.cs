using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class TagEntityMap : WorkspaceEntityMap<Tag, int>
    {
        public override void Configure(EntityTypeBuilder<Tag> builder)
        {
            base.Configure(builder);

            builder
                .Property(tag => tag.Name)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .HasIndex(tag => new { tag.Name, tag.WorkspaceId })
                .IsUnique();
        }
    }
}
