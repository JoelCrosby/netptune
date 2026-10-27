using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Projects;
using Netptune.Handlers.Projects.Commands;
using Netptune.Handlers.Projects.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class ProjectChangeToolTests
{
    private const int ApolloId = 3;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public ProjectChangeToolTests()
    {
        var projects = new List<ProjectViewModel> { new() { Id = ApolloId, Name = "Apollo", Key = "apollo" } };

        Mediator
            .Send(Arg.Any<GetProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(projects);
    }

    [Fact]
    public async Task Delete_ShouldProposeTheDeletion()
    {
        var result = await Execute($$"""{"action":"delete","projectId":{{ApolloId}},"reason":"Finished"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be(DeleteProjectAction.Change);
        change.EntityId.Should().Be(ApolloId);
        change.Summary.Should().Be("Delete project “Apollo”");
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenTheProjectIsNotInTheWorkspace()
    {
        var result = await Execute("""{"action":"delete","projectId":404}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyDelete_ShouldReportTheCommandsFailure()
    {
        Mediator
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Failed("Nope"));

        var handler = new DeleteProjectChangeHandler(Mediator);
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = DeleteProjectAction.Change,
            EntityType = "project",
            Summary = "Delete a project",
            Payload = JsonDocument.Parse($$"""{"projectId":{{ApolloId}}}"""),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };

        var context = new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal),
        };

        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Be("Nope");
    }

    [Fact]
    public async Task Update_ShouldLeaveABlankNameOutOfThePayload_SinceTheReviewDoesNotShowIt()
    {
        var result = await Execute($$"""{"action":"update","projectId":{{ApolloId}},"name":"","description":"New text"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Single();
        var payload = change.Payload.RootElement;

        change.Fields.Select(field => field.Name).Should().Equal("description");
        payload.GetProperty("projectId").GetInt32().Should().Be(ApolloId);
        payload.GetProperty("description").GetString().Should().Be("New text");
        payload.TryGetProperty("name", out _).Should().BeFalse();
    }

    private async Task<AiToolExecution> Execute(string json)
    {
        var tool = new ProjectChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse(json).RootElement;

        return await tool.Execute(arguments, TestContext.Current.CancellationToken);
    }
}
