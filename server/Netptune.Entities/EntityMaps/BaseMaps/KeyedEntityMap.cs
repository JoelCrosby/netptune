using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps
{
    public class KeyedEntityMap<TEntity, TValue> : IEntityTypeConfiguration<TEntity>
        where TEntity : class, IKeyedEntity<TValue>
        where TValue : struct
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(entity => entity.Id);

            builder
                .Property(entity => entity.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
