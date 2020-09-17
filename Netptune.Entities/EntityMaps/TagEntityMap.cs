using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;

namespace Netptune.Entities.EntityMaps.BaseMaps
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
                .HasAlternateKey(tag => new { tag.Name, tag.WorkspaceId });
        }
    }
}
