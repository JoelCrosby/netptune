using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.UnitTests.Netptune.Entities;

public sealed class ProjectTaskEntityModelTests
{
    [Fact]
    public void Tasks_ShouldOnlyReferenceUsersThroughExplicitProperties()
    {
        using var context = new DataContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var task = model.FindEntityType(typeof(ProjectTask))!;
        var userForeignKeys = task.GetForeignKeys()
            .Where(key => key.PrincipalEntityType.ClrType == typeof(AppUser))
            .SelectMany(key => key.Properties)
            .Select(property => property.Name);

        task.GetProperties().Should().NotContain(property => property.IsShadowProperty());
        userForeignKeys.Should().BeEquivalentTo("OwnerId", "CreatedByUserId", "ModifiedByUserId", "DeletedByUserId");
        task.GetIndexes().Should().NotContain(index => index.GetDatabaseName() == "ix_project_tasks_name");
    }
}
