using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class PageVersionEntityMap : KeyedEntityMap<PageVersion, int>
{
    public override void Configure(EntityTypeBuilder<PageVersion> builder)
    {
        base.Configure(builder);

        builder
            .Property(version => version.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(version => version.Summary)
            .HasMaxLength(256);

        builder
            .Property(version => version.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder
            .Property(version => version.CreatedByUserId)
            .IsRequired();

        builder
            .HasOne(version => version.Page)
            .WithMany()
            .HasForeignKey(version => version.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(version => version.CreatedByUser)
            .WithMany()
            .HasForeignKey(version => version.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(version => new
            {
                version.PageId,
                version.Revision
            })
            .HasDatabaseName("ix_page_versions_page_revision");
    }
}
