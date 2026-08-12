using FluentAssertions;

using Netptune.Repositories.Common;

using Xunit;

namespace Netptune.IntegrationTests.Storage;

public class AdvisoryLockTests(AdvisoryLockFixture fixture) : IClassFixture<AdvisoryLockFixture>
{
    private const long Key = 987_654_321;

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TryAcquire_ShouldRefuseASecondHolder_UntilTheFirstReleases()
    {
        var advisoryLock = new PostgresAdvisoryLock(fixture.ConnectionFactory);

        var first = await advisoryLock.TryAcquire(Key, CancellationToken);

        first.Should().NotBeNull();

        var contender = await advisoryLock.TryAcquire(Key, CancellationToken);

        contender.Should().BeNull();

        await first!.DisposeAsync();

        var afterRelease = await advisoryLock.TryAcquire(Key, CancellationToken);

        afterRelease.Should().NotBeNull();

        await afterRelease!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquire_ShouldNotContendOverDifferentKeys()
    {
        var advisoryLock = new PostgresAdvisoryLock(fixture.ConnectionFactory);

        var first = await advisoryLock.TryAcquire(Key + 1, CancellationToken);
        var second = await advisoryLock.TryAcquire(Key + 2, CancellationToken);

        first.Should().NotBeNull();
        second.Should().NotBeNull();

        await first!.DisposeAsync();
        await second!.DisposeAsync();
    }
}
