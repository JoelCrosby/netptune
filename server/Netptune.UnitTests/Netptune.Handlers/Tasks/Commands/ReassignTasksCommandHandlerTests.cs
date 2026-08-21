using AutoFixture;

using FluentAssertions;

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
    private readonly Fixture Fixture = new();
    private readonly ReassignTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();

    public ReassignTasksCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns("workspace");

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
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWithMany(Arg.Is<Action<ActivityMultipleOptions<AssignActivityMeta>>>(configure =>
            CapturesRecipient(configure, request.AssigneeId)));
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

}
