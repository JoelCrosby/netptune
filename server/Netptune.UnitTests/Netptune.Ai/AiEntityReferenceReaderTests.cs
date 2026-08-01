using FluentAssertions;

using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiEntityReferenceReaderTests
{
    [Fact]
    public void Read_ShouldTakeTasksBySystemId_NotByNumericId()
    {
        const string result =
            """
            {"totalCount":1,"returned":1,"tasks":[{"id":9,"systemId":"NPT-42","name":"Fix the login page"}]}
            """;

        var references = AiEntityReferenceReader.Read("search_tasks", result);

        references.Should().ContainSingle();
        references[0].Type.Should().Be("task");
        references[0].Id.Should().Be("NPT-42", "task routes are keyed on the system id");
        references[0].Name.Should().Be("Fix the login page");
    }

    [Fact]
    public void Read_ShouldTakeNumericIds_ForEverythingElse()
    {
        var references = AiEntityReferenceReader.Read("list_projects", """[{"id":4,"name":"Website","key":"WEB"}]""");

        references.Should().ContainSingle();
        references[0].Type.Should().Be("project");
        references[0].Id.Should().Be("4");
    }

    [Fact]
    public void Read_ShouldIgnoreToolsThatDoNotProduceLinkableEntities()
    {
        var references = AiEntityReferenceReader.Read("list_tags", """[{"id":1,"name":"bug"}]""");

        references.Should().BeEmpty("tags have no detail route to link to");
    }

    [Fact]
    public void Read_ShouldIgnoreMalformedResults()
    {
        AiEntityReferenceReader.Read("list_projects", "not json").Should().BeEmpty();
        AiEntityReferenceReader.Read("list_projects", null).Should().BeEmpty();
        AiEntityReferenceReader.Read("list_projects", """[{"id":4}]""").Should().BeEmpty();
    }

    [Fact]
    public void Read_ShouldDeduplicateAcrossInvocations()
    {
        var results = new List<AiToolResultText>
        {
            new() { ToolName = "list_projects", Content = """[{"id":4,"name":"Website"}]""" },
            new() { ToolName = "list_projects", Content = """[{"id":4,"name":"Website"},{"id":5,"name":"Api"}]""" },
        };

        var references = AiEntityReferenceReader.Read(results);

        references.Should().HaveCount(2);
        references.Select(reference => reference.Id).Should().BeEquivalentTo(["4", "5"]);
    }

    [Fact]
    public void Read_ShouldNotConfuseTypesBetweenTools()
    {
        var results = new List<AiToolResultText>
        {
            new() { ToolName = "list_sprints", Content = """[{"id":7,"name":"Sprint 7"}]""" },
            new() { ToolName = "list_boards", Content = """[{"id":7,"name":"Delivery"}]""" },
        };

        var references = AiEntityReferenceReader.Read(results);

        references.Should().HaveCount(2, "the same id under two types is two entities");
        references.Should().Contain(reference => reference.Type == "sprint" && reference.Id == "7");
        references.Should().Contain(reference => reference.Type == "board" && reference.Id == "7");
    }
}
