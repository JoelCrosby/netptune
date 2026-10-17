using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class SourceControlCheckRunEntityMap : KeyedEntityMap<SourceControlCheckRun, long>
{
    public override void Configure(EntityTypeBuilder<SourceControlCheckRun> builder)
    {
        base.Configure(builder);

        builder
            .Property(checkRun => checkRun.ExternalId)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(checkRun => checkRun.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(checkRun => checkRun.CommitSha)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(checkRun => checkRun.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(checkRun => checkRun.Conclusion)
            .HasConversion<int?>();

        builder
            .Property(checkRun => checkRun.WebUrl)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .HasOne(checkRun => checkRun.Repo)
            .WithMany()
            .HasForeignKey(checkRun => checkRun.RepoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(checkRun => new { checkRun.RepoId, checkRun.ExternalId })
            .IsUnique()
            .HasDatabaseName("ix_source_control_check_runs_repo_external_id");

        builder
            .HasIndex(checkRun => new { checkRun.RepoId, checkRun.CommitSha })
            .HasDatabaseName("ix_source_control_check_runs_repo_commit_sha");

        builder
            .HasIndex(checkRun => checkRun.UpdatedAt)
            .HasDatabaseName("ix_source_control_check_runs_updated_at");
    }
}
