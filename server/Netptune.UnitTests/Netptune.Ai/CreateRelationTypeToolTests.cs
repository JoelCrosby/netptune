using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Handlers.RelationTypes.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class CreateRelationTypeToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public CreateRelationTypeToolTests()
    {
        GivenRelationTypes(Blocks());
    }

    [Fact]
    public async Task Execute_ShouldProposeTheRelationType()
    {
        var result = await Execute(
            """{"name":"Duplicates","inverseName":"Is Duplicated By","category":"Duplicate","color":"purple"}""");

        result.IsError.Should().BeFalse();
        result.Content.Should().Contain("ref:1");
        ChangeSet.Changes.Should().ContainSingle();

        var change = ChangeSet.Changes[0];

        change.ToolName.Should().Be("propose_create_relation_type");
        change.EntityType.Should().Be("relationType");
        change.RefKey.Should().Be("ref:1");
        change.Summary.Should().Be("Create relation type “Duplicates”");
        change.Fields.Should().Contain(field => field.Name == "inverseName" && field.After == "Is Duplicated By");
        change.Payload.RootElement.GetProperty("category").GetString().Should().Be("Duplicate");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheCategoryIsNotKnown()
    {
        var result = await Execute("""{"name":"Duplicates","category":"Copies"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Hierarchy");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheNameIsAlreadyTaken()
    {
        var result = await Execute("""{"name":"blocks","category":"Dependency"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheNameMatchesAnInverseName()
    {
        var result = await Execute("""{"name":"Is Blocked By","category":"Dependency"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheSameTypeIsProposedTwice()
    {
        await Execute("""{"name":"Duplicates","category":"Duplicate"}""");

        var result = await Execute("""{"name":"duplicates","category":"Duplicate"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().ContainSingle();
    }

    private async Task<AiToolExecution> Execute(string json)
    {
        var tool = new CreateRelationTypeTool(Mediator, ChangeSet);

        return await tool.Execute(Arguments(json), TestContext.Current.CancellationToken);
    }

    private void GivenRelationTypes(params RelationTypeViewModel[] relationTypes)
    {
        Mediator
            .Send(Arg.Any<GetRelationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(relationTypes.ToList());
    }

    private static RelationTypeViewModel Blocks()
    {
        return new RelationTypeViewModel
        {
            Id = 6,
            Name = "Blocks",
            InverseName = "Is Blocked By",
            Key = "blocks",
            Category = RelationCategory.Dependency,
        };
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
