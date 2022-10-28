using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps;

public class AuditableEntityMap<TEntity, TId> : KeyedEntityMap<TEntity, TId>
    where TEntity : class, IAuditableEntity<TId>
    where TId : struct
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder
            .Property(entity => entity.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder
            .Property(entity => entity.UpdatedAt)
            .IsRequired(false);

        builder
            .Property(entity => entity.IsDeleted)
            .HasDefaultValue(false);

        builder
            .HasIndex(entity => entity.IsDeleted);

        builder
            .Property(entity => entity.ModifiedByUserId)
            .IsRequired(false);

        builder
            .Property(entity => entity.DeletedByUserId)
            .IsRequired(false);

        builder
            .Property(entity => entity.OwnerId)
            .IsRequired(false);

        // Entity > AppUser

        builder
            .HasOne(entity => entity.CreatedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(entity => entity.ModifiedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(entity => entity.DeletedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.DeletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(entity => entity.Owner)
            .WithMany()
            .HasForeignKey(entity => entity.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
