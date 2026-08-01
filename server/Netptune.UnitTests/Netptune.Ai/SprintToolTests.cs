using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class SprintToolTests
{
    private const int SprintId = 12;
    private const int ProjectId = 3;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    [Fact]
    public async Task UpdateSprint_ShouldFail_WhenTheSprintIsCompleted()
    {
        GivenSprint(CreateSprint(SprintStatus.Completed));

        var tool = new UpdateSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}},"name":"Sprint 5"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("completed");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSprint_ShouldProposeOnlyTheFieldsThatChanged()
    {
        GivenSprint(CreateSprint());

        var tool = new UpdateSprintTool(Mediator, ChangeSet);
        var arguments = Arguments($$"""{"sprintId":{{SprintId}},"name":"Sprint 4","goal":"Ship sprint tools","endDate":"2026-07-21"}""");
        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();

        var change = ChangeSet.Changes[0];

        change.EntityType.Should().Be("sprint");
        change.EntityId.Should().Be(SprintId);
        change.Fields.Select(field => field.Name).Should().BeEquivalentTo(["goal", "endDate"]);
        change.Fields.Single(field => field.Name == "endDate").Before.Should().Be("2026-07-14");
    }

    [Fact]
    public async Task UpdateSprint_ShouldFail_WhenTheNewEndDateFallsBeforeTheStartDate()
    {
        GivenSprint(CreateSprint());

        var tool = new UpdateSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}},"endDate":"2026-06-01"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSprint_ShouldFail_WhenNothingWouldChange()
    {
        GivenSprint(CreateSprint());

        var tool = new UpdateSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}},"name":"Sprint 4"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task StartSprint_ShouldProposeTheStatusChange()
    {
        GivenSprint(CreateSprint());
        GivenActiveSprints([]);

        var tool = new StartSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();
        ChangeSet.Changes[0].Fields.Single().After.Should().Be(nameof(SprintStatus.Active));
    }

    [Fact]
    public async Task StartSprint_ShouldFail_WhenTheSprintIsNotPlanning()
    {
        GivenSprint(CreateSprint(SprintStatus.Active));

        var tool = new StartSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task StartSprint_ShouldFail_WhenTheProjectIsAlreadyRunningASprint()
    {
        GivenSprint(CreateSprint());
        GivenActiveSprints([CreateActiveSprint()]);

        var tool = new StartSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Sprint 3");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task StartSprint_ShouldPropose_WhenTheChangeSetCompletesTheRunningSprintFirst()
    {
        var active = CreateActiveSprint();

        GivenSprint(CreateSprint());
        GivenActiveSprints([active]);
        GivenProposedClosure(active.Id, CompleteSprintTool.ToolName);

        var tool = new StartSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().HaveCount(2);
    }

    [Fact]
    public async Task CompleteSprint_ShouldFail_WhenTheSprintIsNotActive()
    {
        GivenSprint(CreateSprint());

        var tool = new CompleteSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelSprint_ShouldFail_WhenTheSprintIsAlreadyCancelled()
    {
        GivenSprint(CreateSprint(SprintStatus.Cancelled));

        var tool = new CancelSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSprint_ShouldFail_WhenTheSprintIsActive()
    {
        GivenSprint(CreateSprint(SprintStatus.Active));

        var tool = new DeleteSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSprint_ShouldProposeDeletingACancelledSprint()
    {
        GivenSprint(CreateSprint(SprintStatus.Cancelled));

        var tool = new DeleteSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}}}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();
        ChangeSet.Changes[0].EntityId.Should().Be(SprintId);
    }

    [Fact]
    public async Task AddTasksToSprint_ShouldFail_WhenATaskBelongsToAnotherProject()
    {
        GivenSprint(CreateSprint());
        GivenTask(CreateTask(1, ProjectId));
        GivenTask(CreateTask(2, ProjectId + 1));

        var tool = new AddTasksToSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}},"taskIds":[1,2]}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTasksToSprint_ShouldSkipTasksAlreadyInTheSprint()
    {
        GivenSprint(CreateSprint());
        GivenTask(CreateTask(1, ProjectId, SprintId));
        GivenTask(CreateTask(2, ProjectId));

        var tool = new AddTasksToSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}},"taskIds":[1,2]}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();

        var payload = ChangeSet.Changes[0].Payload.RootElement.GetProperty("taskIds");

        payload.EnumerateArray().Select(item => item.GetInt32()).Should().BeEquivalentTo([2]);
    }

    [Fact]
    public async Task AddTasksToSprint_ShouldFail_WhenEveryTaskIsAlreadyInTheSprint()
    {
        GivenSprint(CreateSprint());
        GivenTask(CreateTask(1, ProjectId, SprintId));

        var tool = new AddTasksToSprintTool(Mediator, ChangeSet);
        var result = await tool.Execute(Arguments($$"""{"sprintId":{{SprintId}},"taskIds":[1]}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    private void GivenSprint(SprintDetailViewModel sprint)
    {
        var response = ClientResponse<SprintDetailViewModel>.Success(sprint);

        Mediator
            .Send(Arg.Is<GetSprintQuery>(query => query.Id == sprint.Id), Arg.Any<CancellationToken>())
            .Returns(response);
    }

    private void GivenActiveSprints(List<SprintViewModel> sprints)
    {
        Mediator
            .Send(Arg.Any<GetSprintsQuery>(), Arg.Any<CancellationToken>())
            .Returns(sprints);
    }

    private void GivenProposedClosure(int sprintId, string toolName)
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = toolName,
            EntityType = "sprint",
            EntityId = sprintId,
            Summary = "Complete the running sprint",
            Payload = JsonDocument.Parse($$"""{"sprintId":{{sprintId}}}"""),
        });
    }

    private void GivenTask(TaskViewModel task)
    {
        Mediator
            .Send(Arg.Is<GetTaskQuery>(query => query.Id == task.Id), Arg.Any<CancellationToken>())
            .Returns(task);
    }

    private static SprintDetailViewModel CreateSprint(SprintStatus status = SprintStatus.Planning)
    {
        return new SprintDetailViewModel
        {
            Id = SprintId,
            Name = "Sprint 4",
            Goal = "Ship reporting",
            Status = status,
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc),
            ProjectId = ProjectId,
            ProjectName = "Netptune",
            ProjectKey = "NPT",
            WorkspaceId = 1,
            TaskCount = 4,
        };
    }

    private static SprintViewModel CreateActiveSprint()
    {
        return new SprintViewModel
        {
            Id = SprintId + 1,
            Name = "Sprint 3",
            Status = SprintStatus.Active,
            StartDate = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ProjectId = ProjectId,
            ProjectName = "Netptune",
            ProjectKey = "NPT",
            WorkspaceId = 1,
        };
    }

    private static TaskViewModel CreateTask(int id, int projectId, int? sprintId = null)
    {
        return new TaskViewModel
        {
            Id = id,
            Name = $"Task {id}",
            SystemId = $"NPT-{id}",
            ProjectId = projectId,
            SprintId = sprintId,
        };
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
