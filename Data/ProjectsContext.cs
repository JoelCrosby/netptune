using Microsoft.EntityFrameworkCore;
using DataPlane.Models;
using System.Linq;

namespace DataPlane.Data
{
    public class ProjectsContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectType> ProjectTypes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Password> Passwords { get; set; }

        public ProjectsContext(DbContextOptions<ProjectsContext> context) : base(context)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>()
                .HasOne(e => e.ProjectType)
                .WithMany(c => c.Projects);

            modelBuilder.Entity<User>()
                .HasOne(e => e.UserPassword)
                .WithOne(c => c.Owner);

        }
    }
}