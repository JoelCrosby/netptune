using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;
using Netptune.Models.Relationships;

namespace Netptune.Entities.EntityMaps
{
    public class ProjectTaskInBoardGroupEntityMap : AuditableEntityMap<ProjectTaskInBoardGroup, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectTaskInBoardGroup> builder)
        {
            base.Configure(builder);

            builder
                .HasAlternateKey(taskInGroup => new
                {
                    taskInGroup.BoardGroupId,
                    taskInGroup.ProjectTaskId
                });

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
}
