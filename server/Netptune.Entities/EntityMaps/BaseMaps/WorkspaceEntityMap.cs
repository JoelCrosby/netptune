using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.BaseEntities;

namespace Netptune.Entities.EntityMaps.BaseMaps;

public class WorkspaceEntityMap<TEntity, TId> : AuditableEntityMap<TEntity, TId>
    where TEntity : class, IWorkspaceEntity<TId>
    where TId : struct
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder
            .Property(entity => entity.WorkspaceId)
            .IsRequired();

        builder
            .HasOne(entity => entity.Workspace)
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}