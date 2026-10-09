using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Sprints.Queries;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class ListRecordsToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Theory]
    [InlineData("statuses", NetptunePermissions.Statuses.Read)]
    [InlineData("tags", NetptunePermissions.Tags.Read)]
    [InlineData("projects", NetptunePermissions.Projects.Read)]
    [InlineData("relation_types", NetptunePermissions.RelationTypes.Read)]
    [InlineData("board_groups", NetptunePermissions.BoardGroups.Read)]
    [InlineData("boards", NetptunePermissions.Boards.Read)]
    [InlineData("sprints", NetptunePermissions.Sprints.Read)]
    [InlineData("members", NetptunePermissions.Members.Read)]
    [InlineData("member_roles", NetptunePermissions.Members.Read)]
    public void GetRequiredPermissions_ShouldAskOnlyForTheKindBeingListed(string kind, string expected)
    {
        var tool = new ListRecordsTool(Mediator);
        var required = tool.GetRequiredPermissions(Arguments($$"""{"kind":"{{kind}}"}"""));

        required.Should().BeEquivalentTo([expected]);
    }

    [Fact]
    public void IsAvailable_ShouldOfferTheTool_WhenAnyKindCanBeRead()
    {
        var tool = new ListRecordsTool(Mediator);
        var permissions = new HashSet<string> { NetptunePermissions.Tags.Read };

        tool.IsAvailable(permissions).Should().BeTrue("a member who can read only tags can still list them");
        tool.IsAvailable(new HashSet<string>()).Should().BeFalse();
    }

    [Theory]
    [InlineData("""{"kind":"tags"}""", "list_records · tags")]
    [InlineData("""{"kind":"labels"}""", "list_records")]
    [InlineData("{}", "list_records")]
    public void DescribeCall_ShouldNameTheKind_OnlyWhenItIsOneTheToolKnows(string json, string expected)
    {
        var tool = new ListRecordsTool(Mediator);

        tool.DescribeCall(Arguments(json)).Should().Be(expected);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheKindIsUnknown()
    {
        var tool = new ListRecordsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"labels"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("statuses, tags, projects");
    }

    [Fact]
    public async Task Execute_ShouldListTagNames()
    {
        Mediator
            .Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns([new TagViewModel { Id = 1, Name = "bug" }, new TagViewModel { Id = 2, Name = "ux" }]);

        var tool = new ListRecordsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"tags"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        result.Content.Should().Be("""["bug","ux"]""");
    }

    [Fact]
    public async Task Execute_ShouldPassTheProjectThrough_WhenListingSprints()
    {
        GetSprintsQuery? captured = null;

        Mediator
            .Send(Arg.Any<GetSprintsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<GetSprintsQuery>();

                return new List<SprintViewModel>();
            });

        var tool = new ListRecordsTool(Mediator);
        var arguments = Arguments("""{"kind":"sprints","projectId":4}""");

        await tool.Execute(arguments, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.ProjectId.Should().Be(4);
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
