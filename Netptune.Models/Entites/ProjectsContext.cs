using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Interfaces;
using Netptune.Models.Models;
using Netptune.Models.Models.Relationships;

namespace Netptune.Models.Entites
{
    public class ProjectsContext : IdentityDbContext
    {

        // Core data models

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectType> ProjectTypes { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Flag> Flags { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Post> Posts { get; set; }

        // relational data models
        public DbSet<WorkspaceAppUser> WorkspaceAppUsers { get; set; }
        public DbSet<WorkspaceProject> WorkspaceProjects { get; set; }
        public DbSet<ProjectUser> ProjectUsers { get; set; }

        public ProjectsContext(DbContextOptions<ProjectsContext> context) : base(context)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            builder.Entity<Project>()
                .HasOne(e => e.ProjectType)
                .WithMany(c => c.Projects);

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
                .HasMany(c => c.ProjectTasks)
                .WithOne(e => e.Workspace)
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
                .WithOne(e => e.Project);
        }

        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AddTimestamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
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