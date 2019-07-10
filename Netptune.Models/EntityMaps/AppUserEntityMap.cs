using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;

namespace Netptune.Entities.EntityMaps
{
    public class AppUserEntityMap : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            // (One-to-One) AppUser > Task

            builder
                .HasMany(c => c.Tasks)
                .WithOne(e => e.Assignee)
                .IsRequired();
        }
    }
}
