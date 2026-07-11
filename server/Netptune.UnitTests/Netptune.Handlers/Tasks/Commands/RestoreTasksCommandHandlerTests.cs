using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class RestoreTasksCommandHandlerTests
{
    private const string WorkspaceSlug = "workspaceSlug";
    private const int WorkspaceId = 7;

    private readonly RestoreTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();

    public RestoreTasksCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity, EventPublisher);

        Identity.GetWorkspaceKey().Returns(WorkspaceSlug);
        UnitOfWork.Workspaces.GetIdBySlug(WorkspaceSlug, Arg.Any<CancellationToken>()).Returns(WorkspaceId);
    }

    [Fact]
    public async Task Restore_ShouldOnlyRestoreDeletedTasksInWorkspace_WhenIdsContainForeignOrLiveTasks()
    {
        var requested = new[] { 1, 2, 3 };
        var deleted = new List<int> { 1, 3 };

        UnitOfWork.Tasks.GetDeletedTaskIdsInWorkspace(requested, WorkspaceId, Arg.Any<CancellationToken>()).Returns(deleted);
        UnitOfWork.Tasks.Restore(deleted, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await Handler.Handle(new RestoreTasksCommand(requested), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Tasks.Received(1).Restore(deleted, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_ShouldLogRestoreActivity_WhenTasksRestored()
    {
        var ids = new[] { 1, 2 };

        UnitOfWork.Tasks.GetDeletedTaskIdsInWorkspace(ids, WorkspaceId, Arg.Any<CancellationToken>()).Returns(ids.ToList());
        UnitOfWork.Tasks.Restore(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(ids.ToList());

        await Handler.Handle(new RestoreTasksCommand(ids), TestContext.Current.CancellationToken);

        Activity.Received(1).LogMany(Arg.Is<Action<ActivityMultipleOptions>>(configure => IsRestoreActivity(configure)));
    }

    [Fact]
    public async Task Restore_ShouldDispatchSearchIndexEventPerTask_WhenTasksRestored()
    {
        var ids = new[] { 1, 2 };

        UnitOfWork.Tasks.GetDeletedTaskIdsInWorkspace(ids, WorkspaceId, Arg.Any<CancellationToken>()).Returns(ids.ToList());
        UnitOfWork.Tasks.Restore(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(ids.ToList());

        await Handler.Handle(new RestoreTasksCommand(ids), TestContext.Current.CancellationToken);

        await EventPublisher.Received(2).Dispatch(Arg.Is<SearchIndexEvent>(searchEvent =>
            searchEvent.Operation == SearchIndexOperation.Index &&
            searchEvent.EntityType == "task" &&
            searchEvent.WorkspaceSlug == WorkspaceSlug));
    }

    [Fact]
    public async Task Restore_ShouldNotLogOrIndex_WhenNoTasksRestored()
    {
        var ids = new[] { 1, 2 };

        UnitOfWork.Tasks.GetDeletedTaskIdsInWorkspace(ids, WorkspaceId, Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.Tasks.Restore(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await Handler.Handle(new RestoreTasksCommand(ids), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        Activity.Received(0).LogMany(Arg.Any<Action<ActivityMultipleOptions>>());
        await EventPublisher.Received(0).Dispatch(Arg.Any<SearchIndexEvent>());
    }

    private static bool IsRestoreActivity(Action<ActivityMultipleOptions> configure)
    {
        var options = new ActivityMultipleOptions();

        configure(options);

        return options.Type == ActivityType.Restore && options.EntityType == EntityType.Task;
    }
}
