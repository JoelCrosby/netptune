using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public sealed class TaskFileEntityMap : KeyedEntityMap<TaskFile, int>
{
    public override void Configure(EntityTypeBuilder<TaskFile> builder)
    {
        base.Configure(builder);

        builder
            .HasIndex(link => new { link.ProjectTaskId, link.WorkspaceFileId })
            .IsUnique();

        builder
            .HasIndex(link => link.WorkspaceId);

        builder
            .HasOne(link => link.Workspace)
            .WithMany()
            .HasForeignKey(link => link.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(link => link.ProjectTask)
            .WithMany(task => task.Files)
            .HasForeignKey(link => link.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(link => link.WorkspaceFile)
            .WithMany(file => file.TaskFiles)
            .HasForeignKey(link => link.WorkspaceFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
