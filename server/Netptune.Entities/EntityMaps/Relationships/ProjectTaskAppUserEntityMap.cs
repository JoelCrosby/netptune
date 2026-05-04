using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public class ProjectTaskAppUserEntityMap : KeyedEntityMap<ProjectTaskAppUser, int>
{
    public override void Configure(EntityTypeBuilder<ProjectTaskAppUser> builder)
    {
        base.Configure(builder);

        builder
            .HasAlternateKey(projectTaskTag => new
            {
                projectTaskTag.UserId,
                projectTaskTag.ProjectTaskId,
            });

        builder
            .HasIndex(projectTaskTag => new { projectTaskTag.ProjectTaskId, projectTaskTag.UserId })
            .HasDatabaseName("ix_project_task_app_users_task_user");

        builder
            .HasOne(projectTaskTag => projectTaskTag.ProjectTask)
            .WithMany(task => task.ProjectTaskAppUsers)
            .HasForeignKey(projectTaskTag => projectTaskTag.ProjectTaskId);

        builder
            .HasOne(taskAppUser => taskAppUser.User)
            .WithMany(user => user.ProjectTaskAppUsers)
            .HasForeignKey(projectTaskTag => projectTaskTag.UserId);
    }
}
