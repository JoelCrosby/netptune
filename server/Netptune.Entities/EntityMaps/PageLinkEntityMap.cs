using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class PageLinkEntityMap : KeyedEntityMap<PageLink, int>
{
    public override void Configure(EntityTypeBuilder<PageLink> builder)
    {
        base.Configure(builder);

        builder
            .Property(link => link.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(link => link.TargetId)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(link => link.Snippet)
            .HasMaxLength(512);

        builder
            .Property(link => link.IsExplicit)
            .HasDefaultValue(false);

        builder
            .HasOne(link => link.Page)
            .WithMany()
            .HasForeignKey(link => link.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(link => link.PageId)
            .HasDatabaseName("ix_page_links_page");

        builder
            .HasIndex(link => new
            {
                link.TargetType,
                link.TargetId
            })
            .HasDatabaseName("ix_page_links_target");
    }
}
