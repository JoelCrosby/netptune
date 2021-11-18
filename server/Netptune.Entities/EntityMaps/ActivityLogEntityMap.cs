using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class ActivityLogEntityMap : AuditableEntityMap<ActivityLog, int>
{
    public override void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        base.Configure(builder);

        builder
            .HasIndex(log => new { log.Type });

        builder
            .HasIndex(log => new { log.EntityType });

        builder
            .HasIndex(log => new { log.EntityId });

        builder
            .HasIndex(log => new { log.Time });

        builder
            .Property(log => log.Type)
            .IsRequired();

        builder
            .Property(log => log.EntityType)
            .IsRequired();

        builder
            .Property(log => log.Time)
            .IsRequired();

        builder
            .HasOne(log => log.User)
            .WithMany()
            .HasForeignKey(task => task.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}