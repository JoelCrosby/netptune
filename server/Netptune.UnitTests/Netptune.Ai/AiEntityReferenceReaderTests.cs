using System.Text.Json;

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
        references[0].RouteId.Should().Be("NPT-42");
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
    public void Read_ShouldRouteProjectsByKey()
    {
        var references = AiEntityReferenceReader.Read("list_projects", """[{"id":4,"name":"Website","key":"WEB"}]""");

        references.Should().ContainSingle().Which.RouteId.Should().Be("WEB", "the project route is keyed on the project key");
    }

    [Fact]
    public void Read_ShouldRouteBoardsByIdentifier()
    {
        var references = AiEntityReferenceReader.Read("list_boards", """[{"id":7,"name":"Delivery","identifier":"delivery"}]""");

        references.Should().ContainSingle().Which.RouteId.Should().Be("delivery", "the board route is keyed on the identifier");
    }

    [Fact]
    public void Read_ShouldRouteSprintsByIdentifier()
    {
        var references = AiEntityReferenceReader.Read("list_sprints", """[{"id":7,"name":"Sprint 7","identifier":"sprint-7"}]""");

        references.Should().ContainSingle().Which.RouteId.Should().Be("sprint-7", "the sprint route is keyed on the identifier");
    }

    [Fact]
    public void Read_ShouldSkipEntitiesWithoutASlug()
    {
        AiEntityReferenceReader.Read("list_projects", """[{"id":4,"name":"Website"}]""").Should().BeEmpty();
        AiEntityReferenceReader.Read("list_boards", """[{"id":7,"name":"Delivery"}]""").Should().BeEmpty();
        AiEntityReferenceReader.Read("list_sprints", """[{"id":7,"name":"Sprint 7"}]""").Should().BeEmpty();
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
            new() { ToolName = "list_projects", Content = """[{"id":4,"name":"Website","key":"WEB"}]""" },
            new() { ToolName = "list_projects", Content = """[{"id":4,"name":"Website","key":"WEB"},{"id":5,"name":"Api","key":"API"}]""" },
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
            new() { ToolName = "list_sprints", Content = """[{"id":7,"name":"Sprint 7","identifier":"sprint-7"}]""" },
            new() { ToolName = "list_boards", Content = """[{"id":7,"name":"Delivery","identifier":"delivery"}]""" },
        };

        var references = AiEntityReferenceReader.Read(results);

        references.Should().HaveCount(2, "the same id under two types is two entities");
        references.Should().Contain(reference => reference.Type == "sprint" && reference.Id == "7");
        references.Should().Contain(reference => reference.Type == "board" && reference.Id == "7");
    }

    [Theory]
    [InlineData("projects", """[{"id":4,"name":"Website","key":"WEB"}]""", "project", "WEB")]
    [InlineData("sprints", """[{"id":7,"name":"Sprint 7","identifier":"sprint-7"}]""", "sprint", "sprint-7")]
    [InlineData("boards", """[{"id":7,"name":"Delivery","identifier":"delivery"}]""", "board", "delivery")]
    public void Read_ShouldTypeListRecordsByTheKindItListed(string kind, string result, string type, string routeId)
    {
        using var arguments = JsonDocument.Parse($$"""{"kind":"{{kind}}"}""");

        var references = AiEntityReferenceReader.Read("list_records", result, arguments);

        references.Should().ContainSingle();
        references[0].Type.Should().Be(type);
        references[0].RouteId.Should().Be(routeId);
    }

    [Fact]
    public void Read_ShouldLinkNothing_ForListRecordsKindsThatAreNotEntities()
    {
        using var tags = JsonDocument.Parse("""{"kind":"tags"}""");

        AiEntityReferenceReader.Read("list_records", """[{"id":1,"name":"bug","key":"BUG"}]""", tags).Should().BeEmpty();
        AiEntityReferenceReader.Read("list_records", """[{"id":4,"name":"Website","key":"WEB"}]""").Should().BeEmpty(
            "without its arguments there is no telling what list_records listed");
    }
}
