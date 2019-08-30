using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps
{
    public class AuditableEntityMap<TEntity, TId> : KeyedEntityMap<TEntity, TId>
        where TEntity : AuditableEntity<TId>
        where TId : struct
    {
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            base.Configure(builder);

            builder
                .Property(entity => entity.Version)
                .IsConcurrencyToken();

            builder
                .Property(entity => entity.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasDefaultValueSql("GetDate()");

            builder
                .Property(entity => entity.UpdatedAt)
                .HasColumnName("UpdatedAt")
                .IsRequired(false);

            builder
                .Property(entity => entity.IsDeleted)
                .HasColumnName("IsDeleted")
                .HasDefaultValue(false);

            // Enity > AppUser

            builder
                .HasOne(entity => entity.CreatedByUser)
                .WithOne()
                .HasForeignKey<TEntity>(entity => entity.CreatedByUserId);

            builder
                .HasOne(entity => entity.ModifiedByUser)
                .WithOne()
                .HasForeignKey<TEntity>(entity => entity.ModifiedByUserId);

            builder
                .HasOne(entity => entity.DeletedByUser)
                .WithOne()
                .HasForeignKey<TEntity>(entity => entity.DeletedByUserId);
        }
    }
}
