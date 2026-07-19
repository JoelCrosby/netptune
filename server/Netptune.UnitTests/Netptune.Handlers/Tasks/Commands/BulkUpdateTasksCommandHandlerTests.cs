using FluentAssertions;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class BulkUpdateTasksCommandHandlerTests
{
    private const int WorkspaceId = 42;

    private readonly BulkUpdateTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public BulkUpdateTasksCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetWorkspaceKey().Returns("workspace");
        UnitOfWork.InvokeTransaction();

        Handler = new BulkUpdateTasksCommandHandler(
            UnitOfWork,
            Identity,
            Substitute.For<ILogger<BulkUpdateTasksCommandHandler>>(),
            Substitute.For<IEventRecordWriter>());
    }

    [Fact]
    public async Task BulkUpdate_ShouldRejectEmptyTaskList()
    {
        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(new BulkUpdateTasksRequest()),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("At least one task is required");
    }

    [Fact]
    public async Task BulkUpdate_ShouldRejectTasksOutsideWorkspace()
    {
        var request = new BulkUpdateTasksRequest { TaskIds = [1, 2] };
        UnitOfWork.Tasks
            .GetValidTaskIdsInWorkspace(
                Arg.Any<IEnumerable<int>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns([1]);

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("2");
        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task BulkUpdate_ShouldReplaceAssigneesWithoutBoardIdentifier()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1],
            AssigneeIds = ["user-1"],
        };
        var task = new ProjectTask
        {
            Id = 1,
            Name = "Task",
            WorkspaceId = WorkspaceId,
            StatusId = AutoFixtures.TaskStatus.Id,
            Status = AutoFixtures.TaskStatus,
            ProjectTaskAppUsers = [],
        };

        UnitOfWork.Tasks
            .GetValidTaskIdsInWorkspace(
                Arg.Any<IEnumerable<int>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns(request.TaskIds);
        UnitOfWork.Tasks
            .GetTasksForUpdate(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([task]);
        UnitOfWork.Users
            .IsUserInWorkspaceRange(
                Arg.Any<IEnumerable<string>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns([new AppUser { Id = "user-1" }]);

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.ProjectTaskAppUsers.Should().ContainSingle(assignment => assignment.UserId == "user-1");
        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task BulkUpdate_ShouldRejectAssigneesOutsideWorkspace()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1],
            AssigneeIds = ["other-workspace-user"],
        };
        var task = new ProjectTask
        {
            Id = 1,
            Name = "Task",
            WorkspaceId = WorkspaceId,
            StatusId = AutoFixtures.TaskStatus.Id,
            Status = AutoFixtures.TaskStatus,
        };

        UnitOfWork.Tasks
            .GetValidTaskIdsInWorkspace(
                Arg.Any<IEnumerable<int>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns(request.TaskIds);
        UnitOfWork.Tasks
            .GetTasksForUpdate(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([task]);
        UnitOfWork.Users
            .IsUserInWorkspaceRange(
                Arg.Any<IEnumerable<string>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not found in the workspace");
        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task>>());
    }
}
