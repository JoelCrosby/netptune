using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class NotificationEntityMap : AuditableEntityMap<Notification, int>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);

        builder
            .HasIndex(n => new { n.UserId, n.WorkspaceId });

        builder
            .HasIndex(n => new { n.UserId, n.WorkspaceId, n.IsRead });

        builder
            .HasIndex(n => n.IsRead);

        builder
            .HasIndex(n => n.CreatedAt);

        builder
            .Property(n => n.UserId)
            .IsRequired();

        builder
            .Property(n => n.IsRead)
            .HasDefaultValue(false);

        builder
            .Property(n => n.Link)
            .IsRequired(false);

        builder
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(n => n.ActivityLog)
            .WithMany()
            .HasForeignKey(n => n.ActivityLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(n => n.Workspace)
            .WithMany()
            .HasForeignKey(n => n.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
