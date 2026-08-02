using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.ViewModels.Relations;
using Netptune.Handlers.Relations.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class UnlinkTasksToolTests
{
    private const string SystemId = "NPT-42";
    private const int RelationId = 7;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    [Fact]
    public async Task ShouldProposeRemovingTheLink()
    {
        GivenRelations([CreateRelation()]);

        var tool = new UnlinkTasksTool(Mediator, ChangeSet);
        var result = await tool.Execute(
            Arguments($$"""{"systemId":"{{SystemId}}","relationId":{{RelationId}}}"""),
            TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();

        var change = ChangeSet.Changes[0];

        change.ToolName.Should().Be("propose_unlink_tasks");
        change.EntityType.Should().Be("task");
        change.EntityId.Should().Be(9);
        change.Summary.Should().Contain(SystemId).And.Contain("NPT-9");
        change.Fields.Single().Before.Should().Contain("NPT-9");
        change.Payload.RootElement.GetProperty("relationId").GetInt32().Should().Be(RelationId);
    }

    [Fact]
    public async Task ShouldFail_WhenTheTaskHasNoSuchRelation()
    {
        GivenRelations([CreateRelation()]);

        var tool = new UnlinkTasksTool(Mediator, ChangeSet);
        var result = await tool.Execute(
            Arguments($$"""{"systemId":"{{SystemId}}","relationId":99}"""),
            TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldFail_WhenTheTaskIsNotInTheWorkspace()
    {
        GivenRelations(null);

        var tool = new UnlinkTasksTool(Mediator, ChangeSet);
        var result = await tool.Execute(
            Arguments($$"""{"systemId":"{{SystemId}}","relationId":{{RelationId}}}"""),
            TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain(SystemId);
        ChangeSet.Changes.Should().BeEmpty();
    }

    private void GivenRelations(List<TaskRelationViewModel>? relations)
    {
        Mediator
            .Send(Arg.Any<GetTaskRelationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(relations);
    }

    private static TaskRelationViewModel CreateRelation()
    {
        return new TaskRelationViewModel
        {
            Id = RelationId,
            RelationTypeId = 2,
            RelationTypeName = "Blocks",
            RelationTypeKey = "blocks",
            Label = "Blocks",
            IsSource = true,
            RelatedTask = new RelatedTaskViewModel
            {
                Id = 9,
                SystemId = "NPT-9",
                Name = "Fix the login page",
                StatusName = "In progress",
            },
        };
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
