using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class BoardEntityMap : WorkspaceEntityMap<Board, int>
    {
        public override void Configure(EntityTypeBuilder<Board> builder)
        {
            base.Configure(builder);

            builder
                .HasAlternateKey(board => board.Identifier);

            builder
                .Property(board => board.Name)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(board => board.Identifier)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(board => board.BoardType)
                .HasDefaultValue(BoardType.UserDefined)
                .IsRequired();

            builder
                .HasMany(board => board.BoardGroups)
                .WithOne(group => group.Board)
                .HasForeignKey(group => group.BoardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(board => board.Project)
                .WithMany(project => project.ProjectBoards)
                .HasForeignKey(board => board.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(board => board.MetaInfo)
                .HasColumnType("jsonb")
                .IsRequired();
        }
    }
}
