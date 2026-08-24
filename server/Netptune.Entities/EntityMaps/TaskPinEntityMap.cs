using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class TaskPinEntityMap : WorkspaceEntityMap<TaskPin, int>
{
    public override void Configure(EntityTypeBuilder<TaskPin> builder)
    {
        base.Configure(builder);

        builder
            .Property(pin => pin.Scope)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(pin => pin.ScopeEntityId)
            .IsRequired();

        builder
            .Property(pin => pin.SortOrder)
            .HasDefaultValue(0d);

        builder
            .HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(pin => pin.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(pin => new
            {
                pin.WorkspaceId,
                pin.Scope,
                pin.ScopeEntityId
            });

        builder
            .HasIndex(pin => pin.ProjectTaskId);

        builder
            .HasIndex(pin => new
            {
                pin.ProjectTaskId,
                pin.ScopeEntityId,
                pin.CreatedByUserId
            })
            .IsUnique()
            .HasFilter("scope = 0 AND NOT is_deleted");

        builder
            .HasIndex(pin => new
            {
                pin.ProjectTaskId,
                pin.Scope,
                pin.ScopeEntityId
            })
            .IsUnique()
            .HasFilter("scope <> 0 AND NOT is_deleted");
    }
}
