using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Netptune.Entities.Interfaces;
using Netptune.Entities.Entites;
using Netptune.Entities.Entites.Relationships;

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
            //optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Netptune;Integrated Security=SSPI;");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            builder.Entity<Project>()
                .HasOne(e => e.Workspace)
                .WithMany(c => c.Projects)
                .Metadata.DeleteBehavior = DeleteBehavior.Restrict;

            // (One-to-One) AppUser > Task

            builder.Entity<AppUser>()
                .HasMany(c => c.Tasks)
                .WithOne(e => e.Assignee)
                .IsRequired();

            // (One-to-One) Project > Task

            builder.Entity<Project>()
                .HasMany(c => c.ProjectTasks)
                .WithOne(e => e.Project);

            // (One-to-One) Workspace > Task

            builder.Entity<Workspace>()
                .HasMany(worspace => worspace.ProjectTasks)
                .WithOne(task => task.Workspace)
                .HasForeignKey(task => task.WorkspaceId)
                .IsRequired();

            // (Many-to-many) Workspace > Project

            builder.Entity<WorkspaceProject>()
                .HasKey(pt => new { pt.WorkspaceId, pt.ProjectId });

            builder.Entity<WorkspaceProject>()
                .HasOne(pt => pt.Workspace)
                .WithMany(p => p.WorkspaceProjects)
                .HasForeignKey(pt => pt.WorkspaceId);

            builder.Entity<WorkspaceProject>()
                .HasOne(pt => pt.Project)
                .WithMany(t => t.WorkspaceProjects)
                .HasForeignKey(pt => pt.ProjectId);

            // (Many-to-many) Workspace > AppUser

            builder.Entity<WorkspaceAppUser>()
                .HasKey(pt => new { pt.WorkspaceId, pt.UserId });

            builder.Entity<WorkspaceAppUser>()
                .HasOne(pt => pt.Workspace)
                .WithMany(p => p.WorkspaceUsers)
                .HasForeignKey(pt => pt.WorkspaceId);

            builder.Entity<WorkspaceAppUser>()
                .HasOne(pt => pt.User)
                .WithMany(t => t.WorkspaceUsers)
                .HasForeignKey(pt => pt.UserId);

            // (Many-to-many) Project > AppUser

            builder.Entity<ProjectUser>()
                .HasKey(pt => new { pt.ProjectId, pt.UserId });

            builder.Entity<ProjectUser>()
                .HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectUsers)
                .HasForeignKey(pt => pt.ProjectId);

            builder.Entity<ProjectUser>()
                .HasOne(pt => pt.User)
                .WithMany(t => t.ProjectUsers)
                .HasForeignKey(pt => pt.UserId);

            // (One-to-One) Project > Post

            builder.Entity<Project>()
                .HasMany(c => c.ProjectPosts)
                .WithOne(e => e.Project)
                .HasForeignKey(P => P.ProjectId);
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
                x => x.Entity is BaseModel && (x.State == EntityState.Added || x.State == EntityState.Modified)
            );

            foreach (var entity in entities)
            {
                if (entity.Entity is IBaseEntity baseEntity)
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