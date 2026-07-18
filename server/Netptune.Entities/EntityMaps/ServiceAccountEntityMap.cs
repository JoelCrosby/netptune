using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ServiceAccountEntityMap : KeyedEntityMap<ServiceAccount, int>
{
    public override void Configure(EntityTypeBuilder<ServiceAccount> builder)
    {
        base.Configure(builder);

        builder.Property(account => account.Description).HasMaxLength(2048);
        builder.Property(account => account.CreatedAt).IsRequired();
        builder.Property(account => account.DisabledAt).IsRequired(false);

        builder.HasIndex(account => account.UserId).IsUnique();
        builder.HasIndex(account => new { account.WorkspaceId, account.DisabledAt, account.Id });

        builder
            .HasOne(account => account.User)
            .WithOne(user => user.ServiceAccount)
            .HasForeignKey<ServiceAccount>(account => account.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(account => account.CreatedByUser)
            .WithMany()
            .HasForeignKey(account => account.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(account => account.Workspace)
            .WithMany()
            .HasForeignKey(account => account.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
