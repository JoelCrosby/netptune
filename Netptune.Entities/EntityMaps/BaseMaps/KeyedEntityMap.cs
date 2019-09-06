using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Models.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps
{
    public class KeyedEntityMap<TEntity, TValue> : IEntityTypeConfiguration<TEntity>
        where TEntity : KeyedEntity<TValue>
        where TValue : struct
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(entity => entity.Id);

            builder
                .Property(entity => entity.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();
        }
    }
}
