using FluentAssertions;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
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
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly ITaskPlacementService Placement = Substitute.For<ITaskPlacementService>();
    private readonly ITaskReferenceResolver ReferenceResolver = Substitute.For<ITaskReferenceResolver>();

    public BulkUpdateTasksCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetWorkspaceKey().Returns("workspace");
        UnitOfWork.InvokeTransaction();

        ReferenceResolver
            .ResolveAssignees(Arg.Any<IReadOnlyCollection<string>?>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(TaskAssigneeResolution.Unchanged());
        ReferenceResolver
            .ResolveTags(Arg.Any<IReadOnlyCollection<string>?>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(TaskTagResolution.Unchanged());

        Handler = new BulkUpdateTasksCommandHandler(
            UnitOfWork,
            Identity,
            Substitute.For<ILogger<BulkUpdateTasksCommandHandler>>(),
            Substitute.For<IEventRecordWriter>(),
            EventPublisher,
            Placement,
            ReferenceResolver);
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
        ResolvesAssignees("user-1");

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.ProjectTaskAppUsers.Should().ContainSingle(assignment => assignment.UserId == "user-1");
        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task BulkUpdate_ShouldDispatchSearchIndexEvent_ForEveryUpdatedTask()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1, 2],
            AssigneeIds = ["user-1"],
        };
        var tasks = request.TaskIds.Select(id => new ProjectTask
        {
            Id = id,
            Name = $"Task {id}",
            WorkspaceId = WorkspaceId,
            StatusId = AutoFixtures.TaskStatus.Id,
            Status = AutoFixtures.TaskStatus,
            ProjectTaskAppUsers = [],
        }).ToList();

        UnitOfWork.Tasks
            .GetValidTaskIdsInWorkspace(
                Arg.Any<IEnumerable<int>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns(request.TaskIds);
        UnitOfWork.Tasks
            .GetTasksForUpdate(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(tasks);
        ResolvesAssignees("user-1");

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(searchEvent =>
            searchEvent.Operation == SearchIndexOperation.Index &&
            searchEvent.EntityType == "task" &&
            searchEvent.WorkspaceSlug == "workspace" &&
            searchEvent.EntityIds.SequenceEqual(new[] { 1, 2 })));
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
        ReferenceResolver
            .ResolveAssignees(Arg.Any<IReadOnlyCollection<string>?>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(TaskAssigneeResolution.Failed(
                "Assignees were not found in the workspace: other-workspace-user"));

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not found in the workspace");
        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task BulkUpdate_ShouldAddAssignees_WithoutRemovingExistingOnes()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1],
            AssigneeIds = ["user-2"],
            AssigneeMode = BulkCollectionMode.Add,
        };
        var task = BuildTask(1);

        task.ProjectTaskAppUsers = [new ProjectTaskAppUser { ProjectTaskId = 1, UserId = "user-1" }];

        StubTasks(request.TaskIds, task);
        ResolvesAssignees("user-2");

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.ProjectTaskAppUsers
            .Select(assignment => assignment.UserId)
            .Should()
            .BeEquivalentTo(["user-1", "user-2"]);
    }

    [Fact]
    public async Task BulkUpdate_ShouldReplaceTags_WhenModeIsReplace()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1],
            Tags = ["security"],
        };
        var task = BuildTask(1);

        task.ProjectTaskTags = [new ProjectTaskTag { ProjectTaskId = 1, TagId = 7 }];

        StubTasks(request.TaskIds, task);
        ResolvesTags(new Tag { Id = 9, Name = "security" });

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.ProjectTaskTags.Select(link => link.TagId).Should().BeEquivalentTo([9]);
    }

    [Fact]
    public async Task BulkUpdate_ShouldAddTags_AlongsideExistingOnes()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1],
            Tags = ["security"],
            TagMode = BulkCollectionMode.Add,
        };
        var task = BuildTask(1);

        task.ProjectTaskTags = [new ProjectTaskTag { ProjectTaskId = 1, TagId = 7 }];

        StubTasks(request.TaskIds, task);
        ResolvesTags(new Tag { Id = 9, Name = "security" });

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.ProjectTaskTags.Select(link => link.TagId).Should().BeEquivalentTo([7, 9]);
    }

    [Fact]
    public async Task BulkUpdate_ShouldClearDueDate()
    {
        var request = new BulkUpdateTasksRequest { TaskIds = [1], ClearDueDate = true };
        var task = BuildTask(1);

        task.DueDate = new DateOnly(2026, 9, 12);

        StubTasks(request.TaskIds, task);

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task BulkUpdate_ShouldRejectDueDateAndClearDueDateTogether()
    {
        var request = new BulkUpdateTasksRequest
        {
            TaskIds = [1],
            DueDate = new DateOnly(2026, 9, 12),
            ClearDueDate = true,
        };

        StubTasks(request.TaskIds, BuildTask(1));

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("DueDate and ClearDueDate cannot both be supplied");
        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task BulkUpdate_ShouldRejectDueDateBeforeATasksStartDate()
    {
        var request = new BulkUpdateTasksRequest { TaskIds = [1], DueDate = new DateOnly(2026, 9, 12) };
        var task = BuildTask(1);

        task.StartDate = new DateOnly(2026, 9, 20);

        StubTasks(request.TaskIds, task);

        var result = await Handler.Handle(
            new BulkUpdateTasksCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ProjectTaskSchedule.InvalidDateRangeMessage);
        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task>>());
    }

    private static ProjectTask BuildTask(int id)
    {
        return new ProjectTask
        {
            Id = id,
            Name = $"Task {id}",
            WorkspaceId = WorkspaceId,
            StatusId = AutoFixtures.TaskStatus.Id,
            Status = AutoFixtures.TaskStatus,
            ProjectTaskAppUsers = [],
            ProjectTaskTags = [],
        };
    }

    private void StubTasks(List<int> taskIds, params ProjectTask[] tasks)
    {
        UnitOfWork.Tasks
            .GetValidTaskIdsInWorkspace(
                Arg.Any<IEnumerable<int>>(),
                WorkspaceId,
                Arg.Any<CancellationToken>())
            .Returns(taskIds);
        UnitOfWork.Tasks
            .GetTasksForUpdate(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(tasks.ToList());
    }

    private void ResolvesAssignees(params string[] userIds)
    {
        ReferenceResolver
            .ResolveAssignees(Arg.Any<IReadOnlyCollection<string>?>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new TaskAssigneeResolution { ShouldUpdate = true, UserIds = userIds });
    }

    private void ResolvesTags(params Tag[] tags)
    {
        ReferenceResolver
            .ResolveTags(Arg.Any<IReadOnlyCollection<string>?>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new TaskTagResolution { ShouldUpdate = true, Tags = tags });
    }
}
