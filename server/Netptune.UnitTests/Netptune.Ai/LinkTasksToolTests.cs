using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Handlers.RelationTypes.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class LinkTasksToolTests
{
    private const int BlocksId = 6;
    private const int RelatesToId = 7;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public LinkTasksToolTests()
    {
        GivenRelationTypes(Blocks(), RelatesTo());
        GivenTask(557, "NPT-1", "Fix the login page");
        GivenTask(558, "NPT-2", "Ship the release");
    }

    [Fact]
    public async Task Execute_ShouldProposeTheLink_WhenTheRelationTypeIsNamed()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":558,"relationType":"blocks"}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();

        var change = ChangeSet.Changes[0];

        change.ToolName.Should().Be("propose_link_tasks");
        change.EntityId.Should().Be(557);
        change.Summary.Should().Be("Link “NPT-1 · Fix the login page” blocks “NPT-2 · Ship the release”");
        change.Payload.RootElement.GetProperty("sourceSystemId").GetString().Should().Be("NPT-1");
        change.Payload.RootElement.GetProperty("targetSystemId").GetString().Should().Be("NPT-2");
        change.Payload.RootElement.GetProperty("relationTypeId").GetInt32().Should().Be(BlocksId);
    }

    [Fact]
    public async Task Execute_ShouldSwapTheTasks_WhenTheRelationTypeIsNamedByItsInverse()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":558,"relationType":"Is Blocked By"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes[0];

        change.Summary.Should().Be("Link “NPT-2 · Ship the release” blocks “NPT-1 · Fix the login page”");
        change.Payload.RootElement.GetProperty("sourceSystemId").GetString().Should().Be("NPT-2");
        change.Payload.RootElement.GetProperty("targetSystemId").GetString().Should().Be("NPT-1");
    }

    [Fact]
    public async Task Execute_ShouldKeepTheTaskOrder_WhenASymmetricTypeIsNamedByItsInverse()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":558,"relationType":"relates to"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes[0];

        change.Payload.RootElement.GetProperty("sourceSystemId").GetString().Should().Be("NPT-1");
        change.Payload.RootElement.GetProperty("relationTypeId").GetInt32().Should().Be(RelatesToId);
    }

    [Fact]
    public async Task Execute_ShouldListTheRelationTypes_WhenTheIdIsNotInTheWorkspace()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":558,"relationTypeId":1}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Blocks").And.Contain($"{BlocksId}");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldListTheRelationTypes_WhenNoneIsGiven()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":558}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Blocks");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldListTheRelationTypes_WhenTheNameIsUnknown()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":558,"relationType":"caused by"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("caused by").And.Contain("Relates To");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldCarryTheHandle_WhenTheRelationTypeIsProposedInTheSameChangeSet()
    {
        GivenRelationTypes();

        var create = new CreateRelationTypeTool(Mediator, ChangeSet);
        var proposal = await create.Execute(
            Arguments("""{"name":"Duplicates","inverseName":"Is Duplicated By","category":"Duplicate"}"""),
            TestContext.Current.CancellationToken);

        proposal.IsError.Should().BeFalse();

        var refKey = ChangeSet.Changes[0].RefKey;
        var result = await Execute($$"""{"taskId":557,"relatedTaskId":558,"relationTypeRef":"{{refKey}}"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes[1];

        change.Summary.Should().Be("Link “NPT-1 · Fix the login page” duplicates “NPT-2 · Ship the release”");
        change.Payload.RootElement.GetProperty("relationTypeRef").GetString().Should().Be(refKey);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheTaskIsLinkedToItself()
    {
        var result = await Execute("""{"taskId":557,"relatedTaskId":557,"relationType":"blocks"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    private async Task<AiToolExecution> Execute(string json)
    {
        var tool = new LinkTasksTool(Mediator, ChangeSet);

        return await tool.Execute(Arguments(json), TestContext.Current.CancellationToken);
    }

    private void GivenRelationTypes(params RelationTypeViewModel[] relationTypes)
    {
        Mediator
            .Send(Arg.Any<GetRelationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(relationTypes.ToList());
    }

    private void GivenTask(int id, string systemId, string name)
    {
        Mediator
            .Send(new GetTaskQuery(id), Arg.Any<CancellationToken>())
            .Returns(new TaskViewModel { Id = id, SystemId = systemId, Name = name });
    }

    private static RelationTypeViewModel Blocks()
    {
        return new RelationTypeViewModel
        {
            Id = BlocksId,
            Name = "Blocks",
            InverseName = "Is Blocked By",
            Key = "blocks",
            Category = RelationCategory.Dependency,
        };
    }

    private static RelationTypeViewModel RelatesTo()
    {
        return new RelationTypeViewModel
        {
            Id = RelatesToId,
            Name = "Relates To",
            InverseName = "Relates To",
            Key = "relates-to",
            Category = RelationCategory.Related,
        };
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
