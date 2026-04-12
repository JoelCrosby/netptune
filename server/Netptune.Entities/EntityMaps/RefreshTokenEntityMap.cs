using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;

namespace Netptune.Entities.EntityMaps;

public class RefreshTokenEntityMap : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder
            .HasKey(t => t.Id);

        builder
            .Property(t => t.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(t => t.UserId)
            .IsRequired();

        builder
            .HasIndex(t => t.Token)
            .IsUnique();

        builder
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
