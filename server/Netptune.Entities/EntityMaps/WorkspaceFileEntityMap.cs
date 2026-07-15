using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class WorkspaceFileEntityMap : WorkspaceEntityMap<WorkspaceFile, int>
{
    public override void Configure(EntityTypeBuilder<WorkspaceFile> builder)
    {
        base.Configure(builder);

        builder
            .HasOne(file => file.Workspace)
            .WithMany(workspace => workspace.Files)
            .HasForeignKey(file => file.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(file => file.ContentId)
            .HasMaxLength(12)
            .IsRequired();

        builder
            .Property(file => file.OriginalName)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(file => file.StorageKey)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .Property(file => file.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(file => file.Purpose)
            .HasColumnType("workspace_file_purpose")
            .IsRequired();

        builder
            .Property(file => file.Status)
            .HasColumnType("workspace_file_status")
            .IsRequired();

        builder
            .HasIndex(file => file.StorageKey)
            .IsUnique();

        builder
            .HasIndex(file => file.ContentId)
            .IsUnique();

        builder
            .HasIndex(file => new
            {
                file.WorkspaceId,
                file.CreatedAt
            });

        builder
            .HasIndex(file => new
            {
                file.WorkspaceId,
                file.Purpose
            });

        builder
            .HasIndex(file => new
            {
                file.WorkspaceId,
                file.Status
            });

        builder
            .ToTable(table => table
                .HasCheckConstraint("ck_workspace_files_size_bytes", "size_bytes >= 0"));

    }
}
