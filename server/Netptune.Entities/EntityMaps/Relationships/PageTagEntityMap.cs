using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public sealed class PageTagEntityMap : KeyedEntityMap<PageTag, int>
{
    public override void Configure(EntityTypeBuilder<PageTag> builder)
    {
        base.Configure(builder);

        builder
            .HasIndex(pageTag => new
            {
                pageTag.PageId,
                pageTag.TagId
            })
            .IsUnique()
            .HasDatabaseName("ix_page_tags_page_tag");

        builder
            .HasIndex(pageTag => pageTag.TagId)
            .HasDatabaseName("ix_page_tags_tag");

        builder
            .HasOne(pageTag => pageTag.Page)
            .WithMany(page => page.PageTags)
            .HasForeignKey(pageTag => pageTag.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(pageTag => pageTag.Tag)
            .WithMany()
            .HasForeignKey(pageTag => pageTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
