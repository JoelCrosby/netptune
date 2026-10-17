using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Repos;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public class SourceControlRepoSyncPlannerTests
{
    private static readonly DateTime SyncedAt = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    private static readonly SourceControlConnection Connection = new()
    {
        Id = 7,
        WorkspaceId = 3,
        Provider = SourceControlProvider.GitHub,
        ExternalAccountId = "987654",
        AccountLogin = "acme",
    };

    [Fact]
    public void Plan_ShouldAddRepositoriesSeenForTheFirstTime()
    {
        var plan = SourceControlRepoSyncPlanner.Plan(Connection, [], [Remote("1", "netptune")], SyncedAt);

        var added = plan.Added.Should().ContainSingle().Subject;

        added.WorkspaceId.Should().Be(3);
        added.ConnectionId.Should().Be(7);
        added.ExternalId.Should().Be("1");
        added.FullName.Should().Be("acme/netptune");
        added.IsAccessible.Should().BeTrue();
        added.IsLinked.Should().BeFalse("linking is an explicit choice in the connect flow");
        added.SyncedAt.Should().Be(SyncedAt);
    }

    [Fact]
    public void Plan_ShouldRefreshRenamedRepositoriesInPlaceSoLinksSurvive()
    {
        var existing = Existing("1", "old-name", isLinked: true);

        var plan = SourceControlRepoSyncPlanner.Plan(Connection, [existing], [Remote("1", "new-name")], SyncedAt);

        plan.Added.Should().BeEmpty();
        plan.Refreshed.Should().Be(1);
        existing.Name.Should().Be("new-name");
        existing.FullName.Should().Be("acme/new-name");
        existing.IsLinked.Should().BeTrue();
        existing.SyncedAt.Should().Be(SyncedAt);
    }

    [Fact]
    public void Plan_ShouldRemoveUnlinkedRepositoriesThatAreNoLongerVisible()
    {
        var existing = Existing("1", "netptune", isLinked: false);

        var plan = SourceControlRepoSyncPlanner.Plan(Connection, [existing], [], SyncedAt);

        plan.Removed.Should().ContainSingle().Which.Should().BeSameAs(existing);
    }

    [Fact]
    public void Plan_ShouldKeepLinkedRepositoriesThatAreNoLongerVisibleButMarkThemInaccessible()
    {
        var existing = Existing("1", "netptune", isLinked: true);

        var plan = SourceControlRepoSyncPlanner.Plan(Connection, [existing], [], SyncedAt);

        plan.Removed.Should().BeEmpty();
        plan.MarkedInaccessible.Should().Be(1);
        existing.IsAccessible.Should().BeFalse();
        existing.IsLinked.Should().BeTrue();
    }

    [Fact]
    public void Plan_ShouldRestoreAccessWhenARepositoryReappears()
    {
        var existing = Existing("1", "netptune", isLinked: true) with { IsAccessible = false };

        SourceControlRepoSyncPlanner.Plan(Connection, [existing], [Remote("1", "netptune")], SyncedAt);

        existing.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void Plan_ShouldAddARepositoryListedTwiceOnlyOnce()
    {
        var plan = SourceControlRepoSyncPlanner.Plan(Connection, [], [Remote("1", "netptune"), Remote("1", "netptune")], SyncedAt);

        plan.Added.Should().ContainSingle();
    }

    private static ScmRepository Remote(string externalId, string name)
    {
        return new ScmRepository
        {
            Ref = new ScmRepositoryRef(externalId, "acme", name),
            FullName = $"acme/{name}",
            DefaultBranch = "main",
            WebUrl = $"https://github.com/acme/{name}",
            IsPrivate = true,
        };
    }

    private static SourceControlRepo Existing(string externalId, string name, bool isLinked)
    {
        return new SourceControlRepo
        {
            Id = 100,
            WorkspaceId = 3,
            ConnectionId = 7,
            ExternalId = externalId,
            Owner = "acme",
            Name = name,
            FullName = $"acme/{name}",
            DefaultBranch = "main",
            WebUrl = $"https://github.com/acme/{name}",
            IsAccessible = true,
            IsLinked = isLinked,
        };
    }
}
