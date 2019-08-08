using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps
{
    public class KeyedEntityMap<TEntity, TValue> : IEntityTypeConfiguration<TEntity>
        where TEntity : KeyedEntity<TValue>
        where TValue : struct
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(t => t.Id);

            builder
                .Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
        }
    }
}
