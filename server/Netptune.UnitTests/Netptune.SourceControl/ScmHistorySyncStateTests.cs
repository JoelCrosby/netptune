using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.History;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public class ScmHistorySyncStateTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Of_ShouldBeNotSynced_WhenNoSyncWasRequested()
    {
        var status = ScmHistorySyncState.Of(Repo(), Now);

        status.Should().Be(SourceControlHistorySyncStatus.NotSynced);
    }

    [Fact]
    public void Of_ShouldBeSyncing_WhileARequestIsFresh()
    {
        var repo = Repo(requestedAt: Now.AddMinutes(-1));

        var status = ScmHistorySyncState.Of(repo, Now);

        status.Should().Be(SourceControlHistorySyncStatus.Syncing);
    }

    [Fact]
    public void Of_ShouldBeSyncing_WhileAWorkerHoldsTheClaim_EvenForAnOldRequest()
    {
        var repo = Repo(requestedAt: Now.AddMinutes(-30), startedAt: Now.AddMinutes(-2));

        var status = ScmHistorySyncState.Of(repo, Now);

        status.Should().Be(SourceControlHistorySyncStatus.Syncing);
    }

    [Fact]
    public void Of_ShouldBeFailed_WhenARequestWasNeverPickedUp()
    {
        var repo = Repo(requestedAt: Now.AddMinutes(-30));

        var status = ScmHistorySyncState.Of(repo, Now);

        status.Should().Be(SourceControlHistorySyncStatus.Failed);
    }

    [Fact]
    public void Of_ShouldBeFailed_WhenTheWorkerDiedHoldingTheClaim()
    {
        var repo = Repo(requestedAt: Now.AddMinutes(-30), startedAt: Now.AddMinutes(-20));

        var status = ScmHistorySyncState.Of(repo, Now);

        status.Should().Be(SourceControlHistorySyncStatus.Failed);
    }

    [Fact]
    public void Of_ShouldBeFailed_WhenTheLastSyncRecordedAnError()
    {
        var repo = Repo(requestedAt: Now.AddMinutes(-5), syncedAt: Now.AddMinutes(-4), error: "GitHub returned 500");

        var status = ScmHistorySyncState.Of(repo, Now);

        status.Should().Be(SourceControlHistorySyncStatus.Failed);
    }

    [Fact]
    public void Of_ShouldBeSynced_WhenTheLastRequestCompleted()
    {
        var repo = Repo(requestedAt: Now.AddMinutes(-5), syncedAt: Now.AddMinutes(-4));

        var status = ScmHistorySyncState.Of(repo, Now);

        status.Should().Be(SourceControlHistorySyncStatus.Synced);
    }

    private static SourceControlRepo Repo(
        DateTime? requestedAt = null,
        DateTime? startedAt = null,
        DateTime? syncedAt = null,
        string? error = null)
    {
        return new SourceControlRepo
        {
            ExternalId = "1",
            Owner = "acme",
            Name = "api",
            FullName = "acme/api",
            DefaultBranch = "main",
            WebUrl = "https://github.com/acme/api",
            HistorySyncRequestedAt = requestedAt,
            HistorySyncStartedAt = startedAt,
            HistorySyncedAt = syncedAt,
            HistorySyncError = error,
        };
    }
}
