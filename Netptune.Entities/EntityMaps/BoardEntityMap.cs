using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;
using Netptune.Models;

namespace Netptune.Entities.EntityMaps
{
    public class BoardEntityMap : AuditableEntityMap<Board, int>
    {
        public override void Configure(EntityTypeBuilder<Board> builder)
        {
            base.Configure(builder);

            builder
                .Property(board => board.Name)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(board => board.Identifier)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .HasOne(board => board.Project)
                .WithMany(project => project.ProjectBoards)
                .HasForeignKey(board => board.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
