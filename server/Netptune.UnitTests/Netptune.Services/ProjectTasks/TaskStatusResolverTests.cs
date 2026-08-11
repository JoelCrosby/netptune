using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.ProjectTasks;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.ProjectTasks;

public class TaskStatusResolverTests
{
    private const int WorkspaceId = 1;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly TaskStatusResolver Resolver;

    public TaskStatusResolverTests()
    {
        Resolver = new TaskStatusResolver(UnitOfWork);
    }

    [Fact]
    public async Task ResolveRequested_ShouldReturnTheWorkspaceStatus()
    {
        var requested = SetupStatus(5, "in-progress");

        var status = await Resolver.ResolveRequested(5, WorkspaceId, TestContext.Current.CancellationToken);

        status.Should().Be(requested);
    }

    [Fact]
    public async Task ResolveRequested_ShouldReturnNull_AndNotFallBack_WhenTheStatusIsNotInTheWorkspace()
    {
        SetupNewStatus();
        UnitOfWork.Statuses.GetInWorkspace(9, WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var status = await Resolver.ResolveRequested(9, WorkspaceId, TestContext.Current.CancellationToken);

        // A status the caller named explicitly is theirs to get wrong, so this is an error for the
        // caller to report rather than something to silently substitute.
        status.Should().BeNull();

        await UnitOfWork.Statuses.DidNotReceive().GetTaskStatusByKey(
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDefault_ShouldPreferTheSuppliedStatus()
    {
        var preferred = SetupStatus(5, "triage");

        SetupNewStatus();

        var status = await Resolver.ResolveDefault(5, WorkspaceId, TestContext.Current.CancellationToken);

        status.Should().Be(preferred);

        await UnitOfWork.Statuses.DidNotReceive().GetTaskStatusByKey(
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDefault_ShouldFallBackToTheNewStatus_WhenTheSuppliedStatusIsGone()
    {
        var newStatus = SetupNewStatus();

        UnitOfWork.Statuses.GetInWorkspace(9, WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var status = await Resolver.ResolveDefault(9, WorkspaceId, TestContext.Current.CancellationToken);

        status.Should().Be(newStatus);
    }

    [Fact]
    public async Task ResolveDefault_ShouldFallBackToTheNewStatus_WhenNothingIsSupplied()
    {
        var newStatus = SetupNewStatus();

        var status = await Resolver.ResolveDefault(null, WorkspaceId, TestContext.Current.CancellationToken);

        status.Should().Be(newStatus);

        await UnitOfWork.Statuses.DidNotReceive().GetInWorkspace(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDefault_ShouldFallBackToTheFirstStatus_WhenTheWorkspaceHasNoNewStatus()
    {
        var firstStatus = AutoFixtures.TaskStatus with { Id = 12, WorkspaceId = WorkspaceId };

        UnitOfWork.Statuses.GetTaskStatusByKey(WorkspaceId, "new", Arg.Any<CancellationToken>()).ReturnsNull();
        UnitOfWork.Statuses.GetFirstTaskStatus(WorkspaceId, Arg.Any<CancellationToken>()).Returns(firstStatus);

        var status = await Resolver.ResolveDefault(null, WorkspaceId, TestContext.Current.CancellationToken);

        status.Should().Be(firstStatus);
    }

    [Fact]
    public async Task ResolveDefault_ShouldReturnNull_WhenTheWorkspaceHasNoTaskStatusAtAll()
    {
        UnitOfWork.Statuses.GetTaskStatusByKey(WorkspaceId, "new", Arg.Any<CancellationToken>()).ReturnsNull();
        UnitOfWork.Statuses.GetFirstTaskStatus(WorkspaceId, Arg.Any<CancellationToken>()).ReturnsNull();

        var status = await Resolver.ResolveDefault(null, WorkspaceId, TestContext.Current.CancellationToken);

        status.Should().BeNull();
    }

    private Status SetupStatus(int id, string key)
    {
        var status = AutoFixtures.TaskStatus with { Id = id, Key = key, WorkspaceId = WorkspaceId };

        UnitOfWork.Statuses.GetInWorkspace(id, WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(status);

        return status;
    }

    private Status SetupNewStatus()
    {
        var newStatus = AutoFixtures.TaskStatus with { Id = 99, Key = "new", WorkspaceId = WorkspaceId };

        UnitOfWork.Statuses.GetTaskStatusByKey(WorkspaceId, "new", Arg.Any<CancellationToken>()).Returns(newStatus);

        return newStatus;
    }
}
