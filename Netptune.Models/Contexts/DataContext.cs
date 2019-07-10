using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Netptune.Entities.Entites;
using Netptune.Entities.Entites.BaseEntities;
using Netptune.Entities.Entites.Relationships;
using Netptune.Entities.EntityMaps;

namespace Netptune.Entities.Contexts
{
    public class DataContext : IdentityDbContext
    {
        // Core data models
        public DbSet<Project> Projects { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Flag> Flags { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Post> Posts { get; set; }

        // relational data models
        public DbSet<WorkspaceAppUser> WorkspaceAppUsers { get; set; }
        public DbSet<WorkspaceProject> WorkspaceProjects { get; set; }
        public DbSet<ProjectUser> ProjectUsers { get; set; }

        public DataContext() : base()
        {
        }

        public DataContext(DbContextOptions<DataContext> context) : base(context)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=localhost\\SQLEXPRESS;Initial Catalog=Netptune;Integrated Security=SSPI;");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder
                .ApplyConfiguration(new AppUserEntityMap())
                .ApplyConfiguration(new FlagEntityMap())
                .ApplyConfiguration(new PostEntityMap())
                .ApplyConfiguration(new ProjectEntityMap())
                .ApplyConfiguration(new ProjectTaskEntityMap())
                .ApplyConfiguration(new WorkspaceEntityMap());

            builder
                .ApplyConfiguration(new ProjectUserEntityMap())
                .ApplyConfiguration(new WorkspaceAppUserEntityMap())
                .ApplyConfiguration(new WorkspaceProjectEntityMap());
        }

        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
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
                x => x.Entity is AuditableEntity<int> && (x.State == EntityState.Added || x.State == EntityState.Modified)
            );

            foreach (var entity in entities)
            {
                if (entity.Entity is AuditableEntity<int> baseEntity)
                {
                    if (entity.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = DateTime.UtcNow;
                    }

                    baseEntity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}