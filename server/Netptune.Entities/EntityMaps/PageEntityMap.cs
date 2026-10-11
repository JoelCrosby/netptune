using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class PageEntityMap : WorkspaceEntityMap<Page, int>
{
    public override void Configure(EntityTypeBuilder<Page> builder)
    {
        base.Configure(builder);

        builder
            .Property(page => page.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(page => page.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(page => page.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(page => page.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(page => page.Icon)
            .HasMaxLength(64);

        builder
            .Property(page => page.SortOrder)
            .HasDefaultValue(0d);

        builder
            .Property(page => page.Revision)
            .HasDefaultValue(0);

        builder
            .Property(page => page.IsTemplate)
            .HasDefaultValue(false);

        builder
            .HasOne(page => page.Parent)
            .WithMany(page => page.Children)
            .HasForeignKey(page => page.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(page => page.Project)
            .WithMany()
            .HasForeignKey(page => page.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(page => page.ReviewedByUser)
            .WithMany()
            .HasForeignKey(page => page.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(page => new
            {
                page.WorkspaceId,
                page.Slug
            })
            .IsUnique()
            .HasDatabaseName("ix_pages_workspace_slug");

        builder
            .HasIndex(page => new
            {
                page.WorkspaceId,
                page.ParentId,
                page.SortOrder
            })
            .HasDatabaseName("ix_pages_workspace_parent_sort");

        builder
            .HasIndex(page => page.ProjectId)
            .HasDatabaseName("ix_pages_project");
    }
}
