using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class TaskServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly TaskService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public TaskServiceTests()
    {
        Service = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description, };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int?>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Service.Create(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace);

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int?>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeIdNotFound()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace);

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsNull();

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }
}
