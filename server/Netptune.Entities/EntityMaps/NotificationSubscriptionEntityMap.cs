using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class NotificationSubscriptionEntityMap : WorkspaceEntityMap<NotificationSubscription, int>
{
    public override void Configure(EntityTypeBuilder<NotificationSubscription> builder)
    {
        base.Configure(builder);

        builder
            .Property(subscription => subscription.Scope)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(subscription => subscription.ScopeEntityId)
            .IsRequired();

        builder
            .Property(subscription => subscription.Events)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(subscription => subscription.UserId)
            .IsRequired();

        builder
            .HasOne(subscription => subscription.User)
            .WithMany()
            .HasForeignKey(subscription => subscription.UserId)
            .HasConstraintName("fk_notification_subscriptions_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(subscription => new
            {
                subscription.WorkspaceId,
                subscription.Scope,
                subscription.ScopeEntityId
            })
            .HasDatabaseName("ix_notification_subscriptions_scope_target");

        builder
            .HasIndex(subscription => new
            {
                subscription.WorkspaceId,
                subscription.UserId,
                subscription.Scope,
                subscription.ScopeEntityId
            })
            .IsUnique()
            .HasDatabaseName("ux_notification_subscriptions_user_scope_target")
            .HasFilter("NOT is_deleted");
    }
}
