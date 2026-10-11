using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class PageFavouriteEntityMap : KeyedEntityMap<PageFavourite, int>
{
    public override void Configure(EntityTypeBuilder<PageFavourite> builder)
    {
        base.Configure(builder);

        builder
            .Property(favourite => favourite.UserId)
            .IsRequired();

        builder
            .Property(favourite => favourite.SortOrder)
            .HasDefaultValue(0d);

        builder
            .HasOne(favourite => favourite.Page)
            .WithMany()
            .HasForeignKey(favourite => favourite.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(favourite => favourite.User)
            .WithMany()
            .HasForeignKey(favourite => favourite.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(favourite => new
            {
                favourite.PageId,
                favourite.UserId
            })
            .IsUnique()
            .HasDatabaseName("ix_page_favourites_page_user");

        builder
            .HasIndex(favourite => favourite.UserId)
            .HasDatabaseName("ix_page_favourites_user");
    }
}
