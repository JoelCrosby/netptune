using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class UserAiCredentialEntityMap : KeyedEntityMap<UserAiCredential, Guid>
{
    public override void Configure(EntityTypeBuilder<UserAiCredential> builder)
    {
        base.Configure(builder);

        builder
            .Property(credential => credential.UserId)
            .IsRequired();

        builder
            .Property(credential => credential.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(credential => credential.Label)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(credential => credential.Secret)
            .HasColumnType("bytea")
            .IsRequired();

        builder
            .Property(credential => credential.SecretHint)
            .HasMaxLength(8)
            .IsRequired();

        builder
            .Property(credential => credential.CreatedAt)
            .IsRequired();

        builder
            .HasIndex(credential => new { credential.UserId, credential.Provider })
            .IsUnique();

        builder
            .HasOne(credential => credential.User)
            .WithMany()
            .HasForeignKey(credential => credential.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
