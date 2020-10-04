using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps
{
    public class ProjectEntityMap<TEntity, TId> : WorkspaceEntityMap<TEntity, TId>
        where TEntity : class, IProjectEntity<TId>
        where TId : struct
    {
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            base.Configure(builder);

            builder
                .Property(entity => entity.ProjectId)
                .IsRequired();

            builder
                .HasOne(entity => entity.Project)
                .WithMany()
                .HasForeignKey(entity => entity.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
