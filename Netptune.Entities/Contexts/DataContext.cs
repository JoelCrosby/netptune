using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Netptune.Entities.Contexts
{
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

        // relational data models
        public DbSet<WorkspaceAppUser> WorkspaceAppUsers { get; set; }
        public DbSet<WorkspaceProject> WorkspaceProjects { get; set; }
        public DbSet<ProjectUser> ProjectUsers { get; set; }
        public DbSet<ProjectTaskInBoardGroup> ProjectTaskInBoardGroups { get; set; }

        public DataContext() { }

        public DataContext(DbContextOptions<DataContext> context) : base(context) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;

            optionsBuilder.UseNpgsql("Host=localhost;Database=neptune;");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            MapIdentityTableNames(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        }

        private static void MapIdentityTableNames(ModelBuilder builder)
        {
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("Claims");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
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
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified);

            var entitiesString = ChangeTracker
                .Entries<IAuditableEntity<string>>()
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified);

            AddTimeStamps(entities);
            AddTimeStamps(entitiesString);
        }

        private static void AddTimeStamps<T>(IEnumerable<EntityEntry<IAuditableEntity<T>>> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    entity.Entity.CreatedAt = DateTime.UtcNow;
                }

                entity.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}