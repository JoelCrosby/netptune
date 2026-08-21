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

    [Fact]
    public void ToSearchDocument_ShouldIndexEditorDescriptionsAsPlainText()
    {
        var task = new TaskViewModel
        {
            Id = 42,
            Name = "Indexed task",
            SystemId = "ACME-7",
            StatusName = "Todo",
            Description = """
                {"time":1755000000000,"blocks":[{"id":"s8yaTRa7jF","type":"paragraph","data":{"text":"Rotate the&nbsp;<code>signing</code> key"}}],"version":"2.31.6"}
                """,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var document = task.ToSearchDocument("workspace");

        document.Description.Should().Be("Rotate the signing key");
    }

    [Fact]
    public void ToSearchDocument_ShouldIndexPlainTextDescriptionsUnchanged()
    {
        var task = new TaskViewModel
        {
            Id = 43,
            Name = "Legacy task",
            SystemId = "ACME-8",
            StatusName = "Todo",
            Description = "Rotate the signing key",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var document = task.ToSearchDocument("workspace");

        document.Description.Should().Be("Rotate the signing key");
    }
}
