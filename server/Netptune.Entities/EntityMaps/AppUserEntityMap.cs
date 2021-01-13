using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Entities.EntityMaps
{
    public class AppUserEntityMap : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder
                .Property(user => user.Firstname)
                .HasMaxLength(256)
                .IsRequired();

            builder
                .Property(user => user.Lastname)
                .HasMaxLength(256)
                .IsRequired();

            builder
                .Property(user => user.PictureUrl)
                .HasMaxLength(2048);

            builder
                .Property(user => user.AuthenticationProvider)
                .HasDefaultValue(AuthenticationProvider.Netptune)
                .IsRequired();

            builder
                .HasMany(user => user.Tasks)
                .WithOne(task => task.Assignee)
                .HasForeignKey(task => task.AssigneeId)
                .IsRequired();
        }
    }
}
