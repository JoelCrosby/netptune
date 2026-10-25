using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiReviewRunEntityMap : WorkspaceEntityMap<AiReviewRun, int>
{
    public override void Configure(EntityTypeBuilder<AiReviewRun> builder)
    {
        base.Configure(builder);

        builder
            .Property(run => run.SubjectType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(run => run.SubjectId)
            .IsRequired();

        builder
            .Property(run => run.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(run => run.CorrelationId)
            .IsRequired();

        builder
            .Property(run => run.ReviewedFingerprint)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder
            .Property(run => run.BaselineFingerprint)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder
            .Property(run => run.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(run => run.Model)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(run => run.Summary)
            .HasColumnType("text");

        builder
            .Property(run => run.ErrorCode)
            .HasMaxLength(64);

        builder
            .HasIndex(run => new { run.SubjectType, run.SubjectId, run.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_ai_review_runs_subject_created");
    }
}

public sealed class AiReviewFindingEntityMap : KeyedEntityMap<AiReviewFinding, long>
{
    public override void Configure(EntityTypeBuilder<AiReviewFinding> builder)
    {
        base.Configure(builder);

        builder
            .Property(finding => finding.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(finding => finding.Category)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(finding => finding.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(finding => finding.Explanation)
            .HasColumnType("text")
            .IsRequired();

        builder
            .Property(finding => finding.Quote)
            .HasColumnType("text");

        builder
            .Property(finding => finding.Suggestion)
            .HasColumnType("jsonb");

        builder
            .Property(finding => finding.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(finding => finding.DismissReason)
            .HasMaxLength(512);

        builder
            .Property(finding => finding.AppliedChange)
            .HasColumnType("jsonb");

        builder
            .HasIndex(finding => new { finding.RunId, finding.Sequence })
            .IsUnique()
            .HasDatabaseName("ix_ai_review_findings_run_sequence");

        builder
            .HasIndex(finding => new { finding.TargetType, finding.TargetId })
            .HasDatabaseName("ix_ai_review_findings_target");

        builder
            .HasOne(finding => finding.Run)
            .WithMany(run => run.Findings)
            .HasForeignKey(finding => finding.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(finding => finding.DecidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AiReviewFindingReferenceEntityMap : KeyedEntityMap<AiReviewFindingReference, long>
{
    public override void Configure(EntityTypeBuilder<AiReviewFindingReference> builder)
    {
        base.Configure(builder);

        builder
            .Property(reference => reference.Role)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(reference => reference.EntityType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .HasIndex(reference => new
            {
                reference.FindingId,
                reference.Role,
                reference.EntityType,
                reference.EntityId,
            })
            .IsUnique()
            .HasDatabaseName("ix_ai_review_finding_references_unique");

        builder
            .HasIndex(reference => new { reference.EntityType, reference.EntityId, reference.Role })
            .HasDatabaseName("ix_ai_review_finding_references_entity");

        builder
            .HasOne(reference => reference.Finding)
            .WithMany(finding => finding.References)
            .HasForeignKey(reference => reference.FindingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
