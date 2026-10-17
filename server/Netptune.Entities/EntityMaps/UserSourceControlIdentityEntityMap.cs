using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class UserSourceControlIdentityEntityMap : KeyedEntityMap<UserSourceControlIdentity, int>
{
    public override void Configure(EntityTypeBuilder<UserSourceControlIdentity> builder)
    {
        base.Configure(builder);

        builder
            .Property(identity => identity.UserId)
            .IsRequired();

        builder
            .Property(identity => identity.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(identity => identity.ExternalId)
            .HasMaxLength(128);

        builder
            .Property(identity => identity.Username)
            .HasMaxLength(256);

        builder
            .Property(identity => identity.NormalizedUsername)
            .HasMaxLength(256);

        builder
            .HasOne(identity => identity.User)
            .WithMany()
            .HasForeignKey(identity => identity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(identity => new { identity.Provider, identity.ExternalId })
            .IsUnique()
            .HasFilter("external_id IS NOT NULL")
            .HasDatabaseName("ix_user_source_control_identities_provider_external_id");

        builder
            .HasIndex(identity => new { identity.Provider, identity.NormalizedUsername })
            .IsUnique()
            .HasFilter("normalized_username IS NOT NULL")
            .HasDatabaseName("ix_user_source_control_identities_provider_username");

        builder
            .HasIndex(identity => identity.UserId)
            .HasDatabaseName("ix_user_source_control_identities_user_id");
    }
}
