using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ScmWebhookDeliveryEntityMap : KeyedEntityMap<ScmWebhookDelivery, long>
{
    public override void Configure(EntityTypeBuilder<ScmWebhookDelivery> builder)
    {
        base.Configure(builder);

        builder
            .Property(delivery => delivery.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(delivery => delivery.DeliveryId)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(delivery => delivery.EventName)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(delivery => delivery.Action)
            .HasMaxLength(64);

        builder
            .Property(delivery => delivery.ExternalAccountId)
            .HasMaxLength(256);

        builder
            .Property(delivery => delivery.Body)
            .HasColumnType("text")
            .IsRequired();

        builder
            .Property(delivery => delivery.LastError)
            .HasColumnType("text");

        builder
            .HasIndex(delivery => new { delivery.Provider, delivery.DeliveryId })
            .IsUnique()
            .HasDatabaseName("ix_scm_webhook_deliveries_provider_delivery");

        builder
            .HasIndex(delivery => delivery.ReceivedAt)
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_scm_webhook_deliveries_unprocessed");

        builder
            .HasIndex(delivery => delivery.ProcessedAt)
            .HasDatabaseName("ix_scm_webhook_deliveries_processed_at");
    }
}
