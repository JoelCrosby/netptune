using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class GetCurrentSprintToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Execute_ShouldListTheRunningSprints_WhenSeveralProjectsHaveOne()
    {
        GivenRunning(CreateSprint(1, projectId: 3), CreateSprint(2, projectId: 4));

        var root = await Execute("{}");

        root.GetProperty("sprints").GetArrayLength().Should().Be(2);
        await Mediator.DidNotReceive().Send(Arg.Any<GetSprintQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldReadTheProjectsSprint_WhenAProjectIsNamed()
    {
        GivenRunning(CreateSprint(2, projectId: 4));

        var detail = new SprintDetailViewModel
        {
            Id = 2,
            Name = "Sprint 2",
            ProjectId = 4,
            ProjectName = "Mobile",
            Tasks = Enumerable.Range(1, 120).Select(id => new TaskViewModel { Id = id, Name = $"Task {id}" }).ToList(),
        };

        Mediator
            .Send(Arg.Is<GetSprintQuery>(query => query.Id == 2), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<SprintDetailViewModel>.Success(detail));

        var root = await Execute("""{"projectId":4}""");

        root.GetProperty("sprint").GetProperty("id").GetInt32().Should().Be(2);
        root.GetProperty("taskCount").GetInt32().Should().Be(120);
        root.GetProperty("tasksTruncated").GetBoolean().Should().BeTrue();
        await Mediator.Received(1).Send(
            Arg.Is<GetSprintsQuery>(query => query.ProjectId == 4 && query.Statuses.Contains(SprintStatus.Active)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldSayThereIsNoSprint_WhenNoneIsRunning()
    {
        GivenRunning();

        var root = await Execute("{}");

        root.GetProperty("sprint").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private void GivenRunning(params SprintViewModel[] sprints)
    {
        Mediator
            .Send(Arg.Any<GetSprintsQuery>(), Arg.Any<CancellationToken>())
            .Returns(sprints.ToList());
    }

    private async Task<JsonElement> Execute(string json)
    {
        var tool = new GetCurrentSprintTool(Mediator);
        var result = await tool.Execute(JsonDocument.Parse(json).RootElement, TestContext.Current.CancellationToken);

        return JsonDocument.Parse(result.Content).RootElement;
    }

    private static SprintViewModel CreateSprint(int id, int projectId)
    {
        return new SprintViewModel
        {
            Id = id,
            Name = $"Sprint {id}",
            Identifier = $"sprint-{id}",
            ProjectId = projectId,
            ProjectName = $"Project {projectId}",
            ProjectKey = $"p{projectId}",
            Status = SprintStatus.Active,
        };
    }
}
