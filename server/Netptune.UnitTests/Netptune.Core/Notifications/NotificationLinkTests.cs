using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Notifications;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Notifications;

public class NotificationLinkTests
{
    [Theory]
    [InlineData(EntityType.Task, "PROJ-42", "/test-workspace/tasks/PROJ-42")]
    [InlineData(EntityType.Board, "board-1", "/test-workspace/boards/board-1")]
    [InlineData(EntityType.BoardGroup, "board-1", "/test-workspace/boards/board-1")]
    [InlineData(EntityType.Project, "PROJ", "/test-workspace/projects/PROJ")]
    [InlineData(EntityType.Sprint, "99", "/test-workspace/sprints/99")]
    [InlineData(EntityType.Status, "99", "/test-workspace/settings/workspace/statuses/99")]
    public void Build_ShouldRouteToEntity_ForSupportedEntityTypes(EntityType entityType, string identifier, string expected)
    {
        var link = NotificationLink.Build("test-workspace", entityType, identifier);

        link.Should().Be(expected);
    }

    [Theory]
    [InlineData(EntityType.Workspace, "1")]
    [InlineData(EntityType.Comment, "1")]
    [InlineData(EntityType.Tag, "1")]
    [InlineData(EntityType.Task, null)]
    [InlineData(EntityType.Board, "")]
    public void Build_ShouldFallBackToWorkspaceRoot_WhenEntityIsNotRoutable(EntityType entityType, string? identifier)
    {
        var link = NotificationLink.Build("test-workspace", entityType, identifier);

        link.Should().Be("/test-workspace");
    }

    [Fact]
    public void Build_ShouldUseSuppliedSlug_SoRenamesAreReflected()
    {
        var link = NotificationLink.Build("renamed-workspace", EntityType.Task, "PROJ-42");

        link.Should().Be("/renamed-workspace/tasks/PROJ-42");
    }
}
