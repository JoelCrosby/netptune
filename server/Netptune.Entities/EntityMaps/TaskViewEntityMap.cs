using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class TaskViewEntityMap : WorkspaceEntityMap<TaskView, int>
{
    public override void Configure(EntityTypeBuilder<TaskView> builder)
    {
        base.Configure(builder);

        builder
            .Property(view => view.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(view => view.Description)
            .HasMaxLength(1024);

        builder
            .Property(view => view.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(view => view.Icon)
            .HasMaxLength(64);

        builder
            .Property(view => view.Definition)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(view => view.IsShared)
            .HasDefaultValue(false);

        builder
            .HasIndex(view => new
            {
                view.WorkspaceId,
                view.Slug
            })
            .IsUnique();
    }
}
