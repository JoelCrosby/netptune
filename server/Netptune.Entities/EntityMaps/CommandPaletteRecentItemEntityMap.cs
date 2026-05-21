using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class CommandPaletteRecentItemEntityMap : KeyedEntityMap<CommandPaletteRecentItem, int>
{
    public override void Configure(EntityTypeBuilder<CommandPaletteRecentItem> builder)
    {
        base.Configure(builder);

        builder
            .Property(item => item.UserId)
            .IsRequired();

        builder
            .Property(item => item.Type)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(item => item.EntityId)
            .HasMaxLength(128);

        builder
            .Property(item => item.Title)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(item => item.Url)
            .HasMaxLength(2048)
            .IsRequired();

        builder
            .HasIndex(item => new { item.UserId, item.WorkspaceId, item.Url })
            .IsUnique();

        builder
            .HasIndex(item => new { item.UserId, item.WorkspaceId, item.LastAccessedAt })
            .HasDatabaseName("ix_command_palette_recent_items_workspace_recent");

        builder
            .HasIndex(item => new { item.UserId, item.LastAccessedAt })
            .HasDatabaseName("ix_command_palette_recent_items_global_recent");

        builder
            .HasOne(item => item.User)
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(item => item.Workspace)
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
