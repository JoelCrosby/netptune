using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Models.Search;
using Netptune.Core.ViewModels.ProjectTasks;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Search;

public sealed class TaskSearchDocumentExtensionsTests
{
    [Fact]
    public void ToSearchDocument_ShouldStoreStableTaskIdentifierParts()
    {
        var task = new TaskViewModel
        {
            Id = 42,
            Name = "Indexed task",
            SystemId = "OLD-7",
            ProjectId = 3,
            ProjectScopeId = 7,
            StatusName = "Todo",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var document = task.ToSearchDocument("workspace");
        var serialized = JsonSerializer.Serialize(document);

        document.TaskId.Should().Be(42);
        document.ProjectId.Should().Be(3);
        document.ProjectScopeId.Should().Be(7);
        serialized.Should().NotContain("SystemId");
        serialized.Should().NotContain("ProjectKey");
        serialized.Should().NotContain("OLD-7");
    }
}
