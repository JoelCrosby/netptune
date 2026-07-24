using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class CreateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();

    public CreateTaskCommandHandlerTests()
    {
        Fixture.Register(() => new DateOnly(2026, 7, 1));
        UnitOfWork.InvokeTransaction();
        Handler = new(
            UnitOfWork,
            Identity,
            Activity,
            EventPublisher,
            EventRecords);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenStartDateIsAfterDueDate()
    {
        var request = new AddProjectTaskRequest
        {
            Name = "Invalid schedule",
            ProjectId = 1,
            StartDate = new DateOnly(2026, 7, 20),
            DueDate = new DateOnly(2026, 7, 19),
        };

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ProjectTaskSchedule.InvalidDateRangeMessage);
    }

    private void SetupStatusDependencies()
    {
        var status = AutoFixtures.TaskStatus with
        {
            Id = 5,
            WorkspaceId = 1,
            Category = StatusCategory.Todo,
        };

        UnitOfWork.Statuses.GetInWorkspace(
            Arg.Any<int>(),
            1,
            Arg.Any<bool>(),
            TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Statuses.GetTaskStatusByKey(1, "new", TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Statuses.GetFirstTaskStatus(1, TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Statuses.GetFirstTaskStatusByCategory(1, Arg.Any<StatusCategory>(), TestContext.Current.CancellationToken)
            .Returns(status);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1)
            .Create();
        var viewModel = new TaskViewModel
        {
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                MaxSortOrder = 7,
            });

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

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
        var request = Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1)
            .Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                MaxSortOrder = 7,
            });

        await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(3).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1)
            .Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .ReturnsNull();

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1)
            .Create();

        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeRefIdNull()
    {
        var request = Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1)
            .Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                MaxSortOrder = 7,
            });

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldLogActivity_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1)
            .Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                MaxSortOrder = 7,
            });

        await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}
