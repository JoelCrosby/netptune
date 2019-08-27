using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;

namespace Netptune.Entities.EntityMaps
{
    public class AppUserEntityMap : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder
                .Property(user => user.FirstName)
                .HasMaxLength(256)
                .IsRequired();

            builder
                .Property(user => user.LastName)
                .HasMaxLength(256)
                .IsRequired();

            builder
                .Property(user => user.PictureUrl)
                .HasMaxLength(2048);

            // (One-to-One) AppUser > Task

            builder
                .HasMany(user => user.Tasks)
                .WithOne(task => task.Assignee)
                .HasForeignKey(task => task.AssigneeId)
                .IsRequired();
        }
    }
}
