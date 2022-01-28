using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
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
            .Property(task => task.Status)
            .HasDefaultValue(ProjectTaskStatus.New)
            .IsRequired();

        builder
            .Property(task => task.IsFlagged)
            .HasDefaultValue(false)
            .IsRequired();

        builder
            .HasOne(task => task.Assignee)
            .WithMany(user => user.Tasks)
            .HasForeignKey(task => task.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

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
            .Property(task => task.OwnerId)
            .IsRequired();

        builder
            .HasIndex(task => task.Name)
            .IsTsVectorExpressionIndex("english");
    }
}
