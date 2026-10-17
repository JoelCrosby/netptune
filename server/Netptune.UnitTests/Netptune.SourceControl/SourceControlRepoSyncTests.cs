using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;
using Netptune.SourceControl.Registry;
using Netptune.SourceControl.Repos;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public class SourceControlRepoSyncTests
{
    private const int ConnectionId = 7;
    private const int WorkspaceId = 3;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ISourceControlConnectionRepository Connections =
        Substitute.For<ISourceControlConnectionRepository>();
    private readonly ISourceControlRepoRepository Repos = Substitute.For<ISourceControlRepoRepository>();
    private readonly ITaskCodeLinkRepository TaskCodeLinks = Substitute.For<ITaskCodeLinkRepository>();
    private readonly FakeSourceControlProvider Provider = new();
    private readonly AdjustableTimeProvider Clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));
    private readonly SourceControlRepoSync Sync;

    public SourceControlRepoSyncTests()
    {
        UnitOfWork.SourceControlConnections.Returns(Connections);
        UnitOfWork.SourceControlRepos.Returns(Repos);
        UnitOfWork.TaskCodeLinks.Returns(TaskCodeLinks);
        Repos.GetForConnection(ConnectionId, Arg.Any<CancellationToken>()).Returns([]);
        TaskCodeLinks
            .GetRepoIdsWithLinks(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var registry = new SourceControlProviderRegistry([Provider]);

        Sync = new SourceControlRepoSync(UnitOfWork, registry, Clock);
    }

    [Fact]
    public async Task Sync_ShouldStoreTheProviderRepositoryListing()
    {
        StubConnection(SourceControlCapabilities.ListRepositories);

        var result = await Sync.Sync(ConnectionId, WorkspaceId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Added.Should().Be(1);

        await Repos.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<SourceControlRepo>>(repos => repos.Single().FullName == "fake-group/fake-repo"),
            Arg.Any<CancellationToken>());

        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_ShouldReportAConnectionOutsideTheWorkspaceAsNotFound()
    {
        var result = await Sync.Sync(ConnectionId, WorkspaceId, TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_ShouldRefuseAConnectionThatCannotListRepositories()
    {
        StubConnection(SourceControlCapabilities.Webhooks);

        var result = await Sync.Sync(ConnectionId, WorkspaceId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        Provider.FakeConnector.TokenRequests.Should().Be(0);
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private void StubConnection(SourceControlCapabilities granted)
    {
        var connection = new SourceControlConnection
        {
            Id = ConnectionId,
            WorkspaceId = WorkspaceId,
            Provider = Provider.Provider,
            ExternalAccountId = "group-1",
            AccountLogin = "fake-group",
            Status = ScmConnectionStatus.Active,
            GrantedCapabilities = granted,
        };

        Connections.GetOwned(ConnectionId, WorkspaceId, Arg.Any<CancellationToken>()).Returns(connection);
    }
}
