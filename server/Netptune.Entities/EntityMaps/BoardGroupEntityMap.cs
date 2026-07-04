using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class BoardGroupEntityMap : WorkspaceEntityMap<BoardGroup, int>
{
    public override void Configure(EntityTypeBuilder<BoardGroup> builder)
    {
        base.Configure(builder);

        builder
            .Property(boardGroup => boardGroup.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .HasOne(boardGroup => boardGroup.Board)
            .WithMany(board => board.BoardGroups)
            .HasForeignKey(board => board.BoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(boardGroup => boardGroup.Status)
            .WithMany()
            .HasForeignKey(boardGroup => boardGroup.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(boardGroup => new { boardGroup.BoardId, boardGroup.IsDeleted, boardGroup.SortOrder, boardGroup.Id })
            .HasDatabaseName("ix_board_groups_board_deleted_sort_id");
    }
}
