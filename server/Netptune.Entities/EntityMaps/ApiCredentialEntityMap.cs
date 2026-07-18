using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ApiCredentialEntityMap : KeyedEntityMap<ApiCredential, Guid>
{
    public override void Configure(EntityTypeBuilder<ApiCredential> builder)
    {
        base.Configure(builder);

        builder.Property(credential => credential.Name).HasMaxLength(128).IsRequired();
        builder.Property(credential => credential.TokenPrefix).HasMaxLength(32).IsRequired();
        builder.Property(credential => credential.SecretHash).HasColumnType("bytea").IsRequired();
        builder.Property(credential => credential.Scopes).HasColumnType("jsonb").IsRequired();
        builder.Property(credential => credential.CreatedAt).IsRequired();
        builder.Property(credential => credential.ExpiresAt).IsRequired();

        builder.HasIndex(credential => credential.TokenPrefix).IsUnique();
        builder.HasIndex(credential => new { credential.ServiceAccountId, credential.RevokedAt, credential.ExpiresAt });

        builder
            .HasOne(credential => credential.ServiceAccount)
            .WithMany(account => account.Credentials)
            .HasForeignKey(credential => credential.ServiceAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(credential => credential.CreatedByUser)
            .WithMany()
            .HasForeignKey(credential => credential.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
