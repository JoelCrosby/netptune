using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Relations;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Handlers.RelationTypes.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class ListRelationsToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task ShouldReadEveryRelationType_WhenNoTypeWasRequested()
    {
        var captured = CaptureQueries();

        StubRelationTypes(
            RelationType(1, "Blocks", "Blocked by", "blocks", 3),
            RelationType(2, "Relates to", "Relates to", "relates", 1));

        var tool = new ListRelationsTool(Mediator);
        var result = await tool.Execute(Arguments("{}"), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured().Select(query => query.Id).Should().Equal(1, 2);

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("totalCount").GetInt32().Should().Be(4);
        content.RootElement.GetProperty("returnedCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ShouldReadOneRelationType_WhenRequestedByName()
    {
        var captured = CaptureQueries();

        StubRelationTypes(
            RelationType(1, "Blocks", "Blocked by", "blocks", 3),
            RelationType(2, "Relates to", "Relates to", "relates", 1));

        var tool = new ListRelationsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"relationType":"blocks"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured().Select(query => query.Id).Should().Equal(1);

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);
        content.RootElement.GetProperty("relations")[0].GetProperty("relationType").GetString().Should().Be("Blocks");
    }

    [Fact]
    public async Task ShouldFail_WhenTheRequestedRelationTypeDoesNotExist()
    {
        CaptureQueries();
        StubRelationTypes(RelationType(1, "Blocks", "Blocked by", "blocks", 3));

        var tool = new ListRelationsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"relationType":"Nonsense"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Nonsense");
        result.Content.Should().Contain("Blocks");
    }

    [Fact]
    public async Task ShouldSpendTheTakeBudgetAcrossRelationTypes()
    {
        var captured = CaptureQueries();

        StubRelationTypes(
            RelationType(1, "Blocks", "Blocked by", "blocks", 3),
            RelationType(2, "Relates to", "Relates to", "relates", 1));

        var tool = new ListRelationsTool(Mediator);
        var result = await tool.Execute(Arguments("""{"take":1}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured().Should().ContainSingle();
        captured()[0].Page.PageSize.Should().Be(1);

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("returnedCount").GetInt32().Should().Be(1);
        content.RootElement.GetProperty("totalCount").GetInt32().Should().Be(4);
    }

    private Func<IReadOnlyList<GetRelationsForTypeQuery>> CaptureQueries()
    {
        var captured = new List<GetRelationsForTypeQuery>();

        Mediator
            .Send(Arg.Any<GetRelationsForTypeQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<GetRelationsForTypeQuery>();

                captured.Add(query);

                return CreatePage();
            });

        return () => captured;
    }

    private static RelationTypeViewModel RelationType(int id, string name, string inverseName, string key, int relationCount)
    {
        return new RelationTypeViewModel
        {
            Id = id,
            Name = name,
            InverseName = inverseName,
            Key = key,
            RelationCount = relationCount,
        };
    }

    private void StubRelationTypes(params RelationTypeViewModel[] relationTypes)
    {
        Mediator
            .Send(Arg.Any<GetRelationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(relationTypes.ToList());
    }

    private static PagedResponse<RelationTypeRelationViewModel> CreatePage()
    {
        var relation = new RelationTypeRelationViewModel
        {
            Id = 1,
            SourceTask = new RelatedTaskViewModel
            {
                Id = 1,
                SystemId = "NPT-1",
                Name = "Ship the assistant",
                StatusName = "In Progress",
            },
            TargetTask = new RelatedTaskViewModel
            {
                Id = 2,
                SystemId = "NPT-2",
                Name = "Write the docs",
                StatusName = "Backlog",
            },
        };

        return new PagedResponse<RelationTypeRelationViewModel>([relation], 1, 25, 1);
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
