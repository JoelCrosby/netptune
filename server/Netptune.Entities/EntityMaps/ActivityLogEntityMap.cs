using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class ActivityLogEntityMap : AuditableEntityMap<ActivityLog, int>
{
    public override void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        base.Configure(builder);

        builder
            .Property(log => log.EventId)
            .IsRequired();

        builder
            .HasIndex(log => log.EventId)
            .IsUnique()
            .HasDatabaseName("ux_activity_logs_event_id");

        builder
            .HasIndex(log => new { log.Type });

        builder
            .HasIndex(log => new { log.EntityType });

        builder
            .HasIndex(log => new { log.EntityId });

        builder
            .HasIndex(log => new { log.OccurredAt });

        builder
            .HasIndex(log => new { log.WorkspaceId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.EntityType, log.EntityId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_entity_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.EntityType, log.TaskId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_task_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.EntityType, log.BoardId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_board_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.EntityType, log.ProjectId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_project_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.EntityType, log.BoardGroupId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_board_group_occurred_id");

        builder
            .HasIndex(log => new { log.EntityType, log.EntityId, log.IsDeleted, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_entity_deleted_occurred_id");

        builder
            .HasIndex(log => new { log.EntityType, log.TaskId, log.IsDeleted, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_task_deleted_occurred_id");

        builder
            .HasIndex(log => new { log.EntityType, log.BoardId, log.IsDeleted, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_board_deleted_occurred_id");

        builder
            .HasIndex(log => new { log.EntityType, log.ProjectId, log.IsDeleted, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_project_deleted_occurred_id");

        builder
            .HasIndex(log => new { log.EntityType, log.BoardGroupId, log.IsDeleted, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_board_group_deleted_occurred_id");

        builder
            .HasIndex(log => new { log.EntityType, log.WorkspaceId, log.IsDeleted, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_entity_workspace_deleted_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.UserId, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_user_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.Type, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_type_occurred_id");

        builder
            .HasIndex(log => new { log.WorkspaceId, log.EntityType, log.OccurredAt, log.Id })
            .HasDatabaseName("ix_activity_logs_workspace_entity_type_occurred_id");

        builder
            .Property(log => log.Type)
            .IsRequired();

        builder
            .Property(log => log.EntityType)
            .IsRequired();

        builder
            .Property(log => log.OccurredAt)
            .IsRequired();

        builder
            .HasOne(log => log.User)
            .WithMany()
            .HasForeignKey(task => task.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
