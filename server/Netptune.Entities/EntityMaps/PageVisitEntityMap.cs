using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class PageVisitEntityMap : KeyedEntityMap<PageVisit, int>
{
    public override void Configure(EntityTypeBuilder<PageVisit> builder)
    {
        base.Configure(builder);

        builder
            .Property(visit => visit.UserId)
            .IsRequired();

        builder
            .Property(visit => visit.VisitedAt)
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(visit => visit.Page)
            .WithMany()
            .HasForeignKey(visit => visit.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(visit => visit.User)
            .WithMany()
            .HasForeignKey(visit => visit.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(visit => new
            {
                visit.PageId,
                visit.UserId
            })
            .IsUnique()
            .HasDatabaseName("ix_page_visits_page_user");

        builder
            .HasIndex(visit => new
            {
                visit.UserId,
                visit.VisitedAt
            })
            .HasDatabaseName("ix_page_visits_user_visited");
    }
}
