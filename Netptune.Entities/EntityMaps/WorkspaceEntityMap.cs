using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class WorkspaceEntityMap : AuditableEntityMap<Workspace, int>
    {
        public override void Configure(EntityTypeBuilder<Workspace> builder)
        {
            base.Configure(builder);

            builder
                .HasAlternateKey(workspace => workspace.Slug);

            builder
                .Property(workspace => workspace.Name)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(workspace => workspace.Description)
                .HasMaxLength(4096);

            builder
                .Property(workspace => workspace.Slug)
                .HasMaxLength(128)
                .IsRequired();

            builder
                .Property(workspace => workspace.MetaInfo)
                .HasColumnType("jsonb")
                .IsRequired();


            // (One-to-One) Workspace > Task

            builder
                .HasMany(workspace => workspace.ProjectTasks)
                .WithOne(task => task.Workspace)
                .HasForeignKey(task => task.WorkspaceId)
                .IsRequired();
        }
    }
}
