using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class ProjectTaskEntityMap : WorkspaceEntityMap<ProjectTask, int>
{
    public override void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        base.Configure(builder);

        builder
            .Property(task => task.ProjectScopeId)
            .IsRequired();

        builder
            .Property(task => task.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(task => task.Description)
            .HasMaxLength(32768);

        builder
            .Property(task => task.StatusId)
            .IsRequired();

        builder
            .HasOne(task => task.Status)
            .WithMany(status => status.ProjectTasks)
            .HasForeignKey(task => task.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(task => task.Priority)
            .HasColumnName("priority")
            .HasConversion<int?>()
            .IsRequired(false);

        builder
            .Property(task => task.EstimateType)
            .HasColumnName("estimate_type")
            .HasConversion<int?>()
            .IsRequired(false);

        builder
            .Property(task => task.EstimateValue)
            .HasColumnName("estimate_value")
            .HasColumnType("numeric(10,2)")
            .IsRequired(false);

        builder
            .Property(task => task.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder
            .Property(task => task.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder
            .HasOne(task => task.Sprint)
            .WithMany(sprint => sprint.ProjectTasks)
            .HasForeignKey(task => task.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasMany(task => task.Tags)
            .WithMany(tag => tag.Tasks)
            .UsingEntity<ProjectTaskTag>(
                b => b.HasOne(m => m.Tag).WithMany(tag => tag.ProjectTaskTags),
                b => b.HasOne(m => m.ProjectTask).WithMany(task => task.ProjectTaskTags));

        builder
            .HasIndex(task => new { task.ProjectId, task.ProjectScopeId })
            .IsUnique();

        builder
            .HasIndex(task => new { task.WorkspaceId, task.IsDeleted, task.UpdatedAt, task.Id })
            .HasDatabaseName("ix_project_tasks_workspace_deleted_updated_id");

        builder
            .HasIndex(task => new { task.WorkspaceId, task.IsDeleted, task.DueDate, task.Id })
            .HasDatabaseName("ix_project_tasks_workspace_deleted_due_date_id");

        builder
            .HasIndex(task => new { task.WorkspaceId, task.ProjectId, task.IsDeleted, task.UpdatedAt, task.Id })
            .HasDatabaseName("ix_project_tasks_workspace_project_deleted_updated_id");

        builder
            .HasIndex(task => new { task.WorkspaceId, task.SprintId, task.IsDeleted, task.UpdatedAt, task.Id })
            .HasDatabaseName("ix_project_tasks_workspace_sprint_deleted_updated_id");

        builder
            .HasIndex(task => new { task.WorkspaceId, task.StatusId, task.IsDeleted, task.UpdatedAt, task.Id })
            .HasDatabaseName("ix_project_tasks_workspace_status_deleted_updated_id");

        builder
            .Property(task => task.OwnerId)
            .IsRequired(false);

        builder
            .HasIndex(task => task.Name)
            .IsTsVectorExpressionIndex("english");
    }
}
