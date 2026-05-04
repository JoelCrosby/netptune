using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public class ProjectTaskInBoardGroupEntityMap : KeyedEntityMap<ProjectTaskInBoardGroup, int>
{
    public override void Configure(EntityTypeBuilder<ProjectTaskInBoardGroup> builder)
    {
        base.Configure(builder);

        builder
            .HasAlternateKey(taskInGroup => new
            {
                taskInGroup.BoardGroupId,
                taskInGroup.ProjectTaskId,
            });

        builder
            .HasIndex(taskInGroup => new { taskInGroup.BoardGroupId, taskInGroup.SortOrder, taskInGroup.ProjectTaskId })
            .HasDatabaseName("ix_project_task_in_board_groups_group_sort_task");

        builder
            .HasIndex(taskInGroup => new { taskInGroup.ProjectTaskId, taskInGroup.BoardGroupId })
            .HasDatabaseName("ix_project_task_in_board_groups_task_group");

        builder
            .HasOne(taskInGroup => taskInGroup.BoardGroup)
            .WithMany(boardGroup => boardGroup.TasksInGroups)
            .HasForeignKey(taskInGroup => taskInGroup.BoardGroupId);

        builder
            .HasOne(taskInGroup => taskInGroup.ProjectTask)
            .WithMany(projectTask => projectTask.ProjectTaskInBoardGroups)
            .HasForeignKey(taskInGroup => taskInGroup.ProjectTaskId);
    }
}
