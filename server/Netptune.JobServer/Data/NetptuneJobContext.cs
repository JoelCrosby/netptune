using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Netptune.JobServer.Data;

public class NetptuneJobContext : IdentityDbContext<IdentityUser>
{
    public NetptuneJobContext() { }

    public NetptuneJobContext(DbContextOptions<NetptuneJobContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder
            .UseNpgsql("Host=localhost;Database=netptune-jobs;Username=postgres;")
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        MapIdentityTableNames(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(NetptuneJobContext).Assembly);
    }

    private static void MapIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<IdentityUser>().ToTable("users");
        builder.Entity<IdentityRole>().ToTable("roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("claims");
        builder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");
    }
}