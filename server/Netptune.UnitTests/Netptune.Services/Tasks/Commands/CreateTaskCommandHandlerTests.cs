using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services.Tasks.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class CreateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel
        {
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>(), Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Create_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace);

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsNull();

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeRefIdNull()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).ReturnsNull();
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldLogActivity_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>(), Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}
