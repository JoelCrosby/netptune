using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Netptune.Models;
using Netptune.Models.BaseEntities;
using Netptune.Models.Relationships;

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

            optionsBuilder.UseSqlServer("Data Source=localhost\\SQLEXPRESS;Initial Catalog=Netptune;Integrated Security=True;");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            MapIdetityTableNames(builder);

            builder
                .ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        }

        private static void MapIdetityTableNames(ModelBuilder builder)
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
            var entities = ChangeTracker.Entries().Where(
                entry => entry.Entity is AuditableEntity<int> && (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            );

            foreach (var entity in entities)
            {
                if (entity.Entity is AuditableEntity<int> auditableEntity)
                {
                    if (entity.State == EntityState.Added)
                    {
                        auditableEntity.CreatedAt = DateTime.UtcNow;
                    }

                    auditableEntity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}