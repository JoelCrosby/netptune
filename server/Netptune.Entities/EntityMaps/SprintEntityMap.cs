using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class SprintEntityMap : WorkspaceEntityMap<Sprint, int>
{
    public override void Configure(EntityTypeBuilder<Sprint> builder)
    {
        base.Configure(builder);

        builder
            .Property(sprint => sprint.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(sprint => sprint.Goal)
            .HasMaxLength(32768);

        builder
            .Property(sprint => sprint.Status)
            .HasDefaultValue(SprintStatus.Planning)
            .IsRequired();

        builder
            .Property(sprint => sprint.StartDate)
            .IsRequired();

        builder
            .Property(sprint => sprint.EndDate)
            .IsRequired();

        builder
            .Property(sprint => sprint.CompletedAt)
            .IsRequired(false);

        builder
            .HasOne(sprint => sprint.Project)
            .WithMany(project => project.Sprints)
            .HasForeignKey(sprint => sprint.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(sprint => sprint.ProjectTasks)
            .WithOne(task => task.Sprint)
            .HasForeignKey(task => task.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(sprint => new { sprint.WorkspaceId, sprint.ProjectId, sprint.Status });

        builder
            .HasIndex(sprint => new { sprint.ProjectId, sprint.Name });

        builder
            .HasIndex(sprint => sprint.ProjectId)
            .IsUnique()
            .HasFilter("status = 'active'::sprint_status AND is_deleted = false");
    }
}
