using Microsoft.EntityFrameworkCore;
using DataPlane.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Threading;
using DataPlane.Interfaces;
using DataPlane.Models.Relationships;

namespace DataPlane.Entites
{
    public class ProjectsContext : IdentityDbContext
    {

        // Core data models

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectType> ProjectTypes { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Flag> Flags { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }

        // relational data models
        public DbSet<WorkspaceAppUser> WorkspaceAppUsers { get; set; }
        public DbSet<WorkspaceProject> WorkspaceProjects { get; set; }
        public DbSet<ProjectUser> ProjectUsers { get; set; }

        public ProjectsContext(DbContextOptions<ProjectsContext> context) : base(context)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>()
                .HasOne(e => e.ProjectType)
                .WithMany(c => c.Projects);

            modelBuilder.Entity<Project>()
                .HasOne(e => e.Workspace)
                .WithMany(c => c.Projects);

            // (One-to-One) AppUser > Task

            modelBuilder.Entity<AppUser>()
                .HasMany(c => c.Tasks)
                .WithOne(e => e.Assignee)
                .IsRequired();

            // (Many-to-many) Workspace > Project

            modelBuilder.Entity<WorkspaceProject>()
                .HasOne(pt => pt.Workspace)
                .WithMany(p => p.WorkspaceProjects)
                .HasForeignKey(pt => pt.WorkspaceId);

            modelBuilder.Entity<WorkspaceProject>()
                .HasOne(pt => pt.Project)
                .WithMany(t => t.WorkspaceProjects)
                .HasForeignKey(pt => pt.ProjectId);

            // (Many-to-many) Workspace > AppUser

            modelBuilder.Entity<WorkspaceAppUser>()
                .HasOne(pt => pt.Workspace)
                .WithMany(p => p.WorkspaceUsers)
                .HasForeignKey(pt => pt.WorkspaceId);

            modelBuilder.Entity<WorkspaceAppUser>()
                .HasOne(pt => pt.User)
                .WithMany(t => t.WorkspaceUsers)
                .HasForeignKey(pt => pt.UserId);

            // (Many-to-many) Project > AppUser

            modelBuilder.Entity<ProjectUser>()
                .HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectUsers)
                .HasForeignKey(pt => pt.ProjectId);

            modelBuilder.Entity<ProjectUser>()
                .HasOne(pt => pt.User)
                .WithMany(t => t.ProjectUsers)
                .HasForeignKey(pt => pt.UserId);

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