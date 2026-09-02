using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class ReassignTasksCommandHandlerTests
{
    private const int WorkspaceId = 42;

    private readonly Fixture Fixture = new();
    private readonly ReassignTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();

    public ReassignTasksCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        UnitOfWork.Users
            .IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var userIds = callInfo.Arg<IEnumerable<string>>();

                return userIds.Select(id => new AppUser { Id = id }).ToList();
            });

        UnitOfWork.Tasks
            .ReplaceTaskAssignees(
                Arg.Any<IEnumerable<int>>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        Handler = new(UnitOfWork, Activity, Identity, EventPublisher);
    }

    [Fact]
    public async Task ReassignTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);

        var result = await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReassignTasks_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReassignTasks_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<ReassignTasksRequest>().With(r => r.AssigneeIds, ["user-1"]).Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWithMany(Arg.Is<Action<ActivityMultipleOptions<AssignActivityMeta>>>(configure =>
            CapturesRecipient(configure, "user-1")));
    }

    [Fact]
    public async Task ReassignTasks_ShouldDispatchSearchIndexEvent_ForTheReassignedTasks()
    {
        var request = Fixture.Build<ReassignTasksRequest>().With(r => r.TaskIds, [1, 2, 3]).Create();

        UnitOfWork.Tasks
            .GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken)
            .Returns(new List<int> { 1, 2 });

        await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(searchEvent =>
            searchEvent.Operation == SearchIndexOperation.Index &&
            searchEvent.EntityType == "task" &&
            searchEvent.WorkspaceSlug == "workspace" &&
            searchEvent.EntityIds.SequenceEqual(new[] { 1, 2 })));
    }

    private static bool CapturesRecipient(
        Action<ActivityMultipleOptions<AssignActivityMeta>> configure,
        string assigneeId)
    {
        var options = new ActivityMultipleOptions<AssignActivityMeta>();
        configure(options);

        return options.RecipientUserIds?.SequenceEqual([assigneeId]) == true;
    }

    [Fact]
    public async Task ReassignTasks_ShouldClearEveryAssignee_WhenNoAssigneeIsNamed()
    {
        var request = Fixture.Build<ReassignTasksRequest>()
            .With(r => r.TaskIds, [1, 2])
            .With(r => r.AssigneeIds, [])
            .Create();

        UnitOfWork.Tasks
            .GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken)
            .Returns(new List<int> { 1, 2 });

        var result = await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Tasks.Received(1).ReplaceTaskAssignees(
            Arg.Any<IEnumerable<int>>(),
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
            Arg.Any<CancellationToken>());
        await UnitOfWork.Users.DidNotReceive().IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReassignTasks_ShouldLogUnassignActivity_ForEveryoneDropped()
    {
        var request = Fixture.Build<ReassignTasksRequest>()
            .With(r => r.TaskIds, [1])
            .With(r => r.AssigneeIds, ["user-1"])
            .Create();

        UnitOfWork.Tasks
            .GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken)
            .Returns(new List<int> { 1 });
        UnitOfWork.Tasks
            .ReplaceTaskAssignees(
                Arg.Any<IEnumerable<int>>(),
                Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("user-1")),
                Arg.Any<CancellationToken>())
            .Returns(["user-2"]);

        await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWithMany(Arg.Is<Action<ActivityMultipleOptions<AssignActivityMeta>>>(configure =>
            CapturesAssignment(configure, ActivityType.Unassign, "user-2")));
        Activity.Received(1).LogWithMany(Arg.Is<Action<ActivityMultipleOptions<AssignActivityMeta>>>(configure =>
            CapturesAssignment(configure, ActivityType.Assign, "user-1")));
    }

    [Fact]
    public async Task ReassignTasks_ShouldRejectAnAssigneeOutsideTheWorkspace()
    {
        var request = Fixture.Build<ReassignTasksRequest>()
            .With(r => r.AssigneeIds, ["other-workspace-user"])
            .Create();

        UnitOfWork.Users
            .IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("other-workspace-user");

        await UnitOfWork.Tasks.DidNotReceive().ReplaceTaskAssignees(
            Arg.Any<IEnumerable<int>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReassignTasks_ShouldAssignEveryonePicked()
    {
        var request = Fixture.Build<ReassignTasksRequest>()
            .With(r => r.TaskIds, [1])
            .With(r => r.AssigneeIds, ["user-1", "user-2"])
            .Create();

        UnitOfWork.Tasks
            .GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken)
            .Returns(new List<int> { 1 });

        var result = await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Tasks.Received(1).ReplaceTaskAssignees(
            Arg.Any<IEnumerable<int>>(),
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "user-1", "user-2" })),
            Arg.Any<CancellationToken>());

        Activity.Received(1).LogWithMany(Arg.Is<Action<ActivityMultipleOptions<AssignActivityMeta>>>(configure =>
            CapturesAssignment(configure, ActivityType.Assign, "user-1")));
        Activity.Received(1).LogWithMany(Arg.Is<Action<ActivityMultipleOptions<AssignActivityMeta>>>(configure =>
            CapturesAssignment(configure, ActivityType.Assign, "user-2")));
    }

    private static bool CapturesAssignment(
        Action<ActivityMultipleOptions<AssignActivityMeta>> configure,
        ActivityType type,
        string assigneeId)
    {
        var options = new ActivityMultipleOptions<AssignActivityMeta>();
        configure(options);

        return options.Type == type && options.Meta?.AssigneeId == assigneeId;
    }
}
