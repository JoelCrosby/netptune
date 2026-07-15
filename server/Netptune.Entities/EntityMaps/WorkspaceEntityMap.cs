using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class WorkspaceEntityMap : AuditableEntityMap<Workspace, int>
{
    public override void Configure(EntityTypeBuilder<Workspace> builder)
    {
        base.Configure(builder);

        builder
            .HasAlternateKey(workspace => workspace.Slug);

        builder
            .Property(workspace => workspace.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(workspace => workspace.Description)
            .HasMaxLength(4096);

        builder
            .Property(workspace => workspace.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(workspace => workspace.MetaInfo)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(workspace => workspace.StorageUsedBytes)
            .HasDefaultValue(0L)
            .IsRequired();

        builder
            .Property(workspace => workspace.StorageLimitBytes)
            .HasDefaultValue(Workspace.DefaultStorageLimitBytes)
            .IsRequired();

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_workspaces_storage_used_bytes", "storage_used_bytes >= 0");
            table.HasCheckConstraint("ck_workspaces_storage_limit_bytes", "storage_limit_bytes >= 0");
        });

        // (One-to-One) Workspace > Task

        builder
            .HasMany(workspace => workspace.Tasks)
            .WithOne(task => task.Workspace)
            .HasForeignKey(task => task.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(workspace => workspace.Users)
            .WithMany(user => user.Workspaces)
            .UsingEntity<WorkspaceAppUser>(
                b => b.HasOne(m => m.User).WithMany(tag => tag.WorkspaceUsers),
                b => b.HasOne(m => m.Workspace).WithMany(task => task.WorkspaceUsers));
    }
}
