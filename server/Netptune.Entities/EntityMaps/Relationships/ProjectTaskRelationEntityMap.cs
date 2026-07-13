using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public sealed class ProjectTaskRelationEntityMap : KeyedEntityMap<ProjectTaskRelation, int>
{
    public override void Configure(EntityTypeBuilder<ProjectTaskRelation> builder)
    {
        base.Configure(builder);

        builder
            .ToTable(table => table.HasCheckConstraint(
                "ck_project_task_relations_no_self_relation",
                "source_task_id <> target_task_id"));

        builder
            .HasIndex(relation => new { relation.SourceTaskId, relation.TargetTaskId, relation.RelationTypeId })
            .IsUnique()
            .HasDatabaseName("ix_project_task_relations_source_target_type");

        // The inverse lookup — a task's relations are found by matching either endpoint.
        builder
            .HasIndex(relation => relation.TargetTaskId)
            .HasDatabaseName("ix_project_task_relations_target");

        builder
            .HasIndex(relation => relation.WorkspaceId)
            .HasDatabaseName("ix_project_task_relations_workspace");

        builder
            .HasOne(relation => relation.Workspace)
            .WithMany()
            .HasForeignKey(relation => relation.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(relation => relation.RelationType)
            .WithMany(relationType => relationType.ProjectTaskRelations)
            .HasForeignKey(relation => relation.RelationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Both endpoints are restricted rather than cascaded — task deletion clears relations
        // explicitly via IProjectTaskRelationRepository.DeleteAllByTaskId, and two cascading
        // paths into the same table is not something every provider will accept.
        builder
            .HasOne(relation => relation.SourceTask)
            .WithMany()
            .HasForeignKey(relation => relation.SourceTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(relation => relation.TargetTask)
            .WithMany()
            .HasForeignKey(relation => relation.TargetTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
