using Microsoft.EntityFrameworkCore;
using DataPlane.Models;

namespace DataPlane.Data
{
    public class ProjectsContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectType> ProjectTypes { get; set; }

        public ProjectsContext(DbContextOptions<ProjectsContext> context) : base(context)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>()
                .HasOne(e => e.ProjectType)
                .WithMany(c => c.Projects);
        }
    }
}