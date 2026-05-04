using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public class ProjectTaskTagEntityMap : KeyedEntityMap<ProjectTaskTag, int>
{
    public override void Configure(EntityTypeBuilder<ProjectTaskTag> builder)
    {
        base.Configure(builder);

        builder
            .HasAlternateKey(projectTaskTag => new
            {
                projectTaskTag.TagId,
                projectTaskTag.ProjectTaskId,
            });

        builder
            .HasIndex(projectTaskTag => new { projectTaskTag.ProjectTaskId, projectTaskTag.TagId })
            .HasDatabaseName("ix_project_task_tags_task_tag");

        builder
            .HasOne(projectTaskTag => projectTaskTag.ProjectTask)
            .WithMany(boardGroup => boardGroup.ProjectTaskTags)
            .HasForeignKey(projectTaskTag => projectTaskTag.ProjectTaskId);

        builder
            .HasOne(projectTaskTag => projectTaskTag.Tag)
            .WithMany(projectTask => projectTask.ProjectTaskTags)
            .HasForeignKey(projectTaskTag => projectTaskTag.TagId);
    }
}
