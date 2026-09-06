using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Entities.Sql;

namespace Netptune.Entities.EntityMaps;

public class AppUserEntityMap : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder
            .Property(user => user.UserType)
            .HasDefaultValue(AppUserType.User)
            .IsRequired();

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
            .Property(user => user.DisplayName)
            .HasComputedColumnSql(SqlScripts.UserDisplayName, stored: true);
    }
}
