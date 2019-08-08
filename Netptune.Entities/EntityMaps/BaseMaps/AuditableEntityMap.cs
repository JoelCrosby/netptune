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
                .Property(t => t.Version)
                .IsConcurrencyToken();

            builder
                .Property(t => t.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasDefaultValueSql("GetDate()");

            builder
                .Property(t => t.UpdatedAt)
                .HasColumnName("UpdatedAt")
                .IsRequired(false);

            builder
                .Property(t => t.IsDeleted)
                .HasColumnName("IsDeleted")
                .HasDefaultValue(false);
        }
    }
}
