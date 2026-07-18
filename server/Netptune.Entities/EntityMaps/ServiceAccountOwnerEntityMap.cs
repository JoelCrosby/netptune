using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;

namespace Netptune.Entities.EntityMaps;

public sealed class ServiceAccountOwnerEntityMap : IEntityTypeConfiguration<ServiceAccountOwner>
{
    public void Configure(EntityTypeBuilder<ServiceAccountOwner> builder)
    {
        builder.HasKey(owner => new { owner.ServiceAccountId, owner.UserId });
        builder.Property(owner => owner.CreatedAt).IsRequired();

        builder
            .HasOne(owner => owner.ServiceAccount)
            .WithMany(account => account.Owners)
            .HasForeignKey(owner => owner.ServiceAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(owner => owner.User)
            .WithMany(user => user.OwnedServiceAccounts)
            .HasForeignKey(owner => owner.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
