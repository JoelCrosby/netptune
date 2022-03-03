using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.Entities.Contexts;

public class DataContext : IdentityDbContext<AppUser>
{
    // Core data models
    public DbSet<Project> Projects { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<Flag> Flags { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Board> Boards { get; set; }
    public DbSet<BoardGroup> BoardGroups { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    // relational data models
    public DbSet<WorkspaceAppUser> WorkspaceAppUsers { get; set; }
    public DbSet<ProjectUser> ProjectUsers { get; set; }
    public DbSet<ProjectTaskInBoardGroup> ProjectTaskInBoardGroups { get; set; }
    public DbSet<ProjectTaskTag> ProjectTaskTags { get; set; }
    public DbSet<ProjectTaskAppUser> ProjectTaskAppUsers { get; set; }

    public DataContext() { }

    public DataContext(DbContextOptions<DataContext> context) : base(context)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder
            .UseNpgsql("Host=localhost;Database=netptune;Username=postgres;")
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        MapIdentityTableNames(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }

    private static void MapIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<IdentityRole>().ToTable("roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("claims");
        builder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");
    }

    public override int SaveChanges()
    {
        AddTimestamps();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AddTimestamps();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddTimestamps();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddTimestamps()
    {
        var entities = ChangeTracker
            .Entries<IAuditableEntity<int>>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        var entitiesString = ChangeTracker
            .Entries<IAuditableEntity<string>>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        AddTimeStamps(entities);
        AddTimeStamps(entitiesString);
    }

    private static void AddTimeStamps<T>(IEnumerable<EntityEntry<IAuditableEntity<T>>> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.State == EntityState.Added && entity.Entity.CreatedAt == default)
            {
                entity.Entity.CreatedAt = DateTime.UtcNow;
            }

            if (entity.State == EntityState.Added && entity.Entity.UpdatedAt != null)
            {
                continue;
            }

            entity.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
