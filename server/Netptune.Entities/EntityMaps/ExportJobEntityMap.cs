using Netptune.Transfer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ExportJobEntityMap : WorkspaceEntityMap<ExportJob, int>
{
    public override void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        base.Configure(builder);

        builder
            .Property(job => job.PublicId)
            .IsRequired();

        builder
            .Property(job => job.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(job => job.RecordType)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(job => job.Format)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(job => job.Definition)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(job => job.RequestedBy)
            .IsRequired();

        builder
            .Property(job => job.Name)
            .HasMaxLength(256);

        builder
            .Property(job => job.StorageKey)
            .HasMaxLength(1024);

        builder
            .Property(job => job.FileName)
            .HasMaxLength(512);

        builder
            .Property(job => job.ContentType)
            .HasMaxLength(255);

        builder
            .Property(job => job.ProgressMessage)
            .HasMaxLength(512);

        builder
            .Property(job => job.Error)
            .HasColumnType("text");

        builder
            .Property(job => job.QuotaReleased)
            .HasDefaultValue(false);

        builder
            .Property(job => job.ExpiresAt)
            .IsRequired();

        builder
            .HasIndex(job => job.PublicId)
            .IsUnique();

        builder
            .HasIndex(job => new
            {
                job.WorkspaceId,
                job.CreatedAt
            });

        builder
            .HasIndex(job => new
            {
                job.WorkspaceId,
                job.Status
            });

        builder
            .HasIndex(job => job.ExpiresAt);

        builder
            .ToTable(table => table
                .HasCheckConstraint("ck_export_jobs_progress_percent", "progress_percent >= 0 AND progress_percent <= 100"));
    }
}
