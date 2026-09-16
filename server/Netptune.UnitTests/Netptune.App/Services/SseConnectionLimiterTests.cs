using FluentAssertions;

using Netptune.App.Services;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Services;

public class SseConnectionLimiterTests
{
    private const string UserId = "user-1";

    private readonly SseConnectionLimiter Limiter = new();

    [Fact]
    public void TryAcquire_ShouldGrantUpToTheLimit_WhenOneUserConnectsRepeatedly()
    {
        var leases = AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser);

        leases.Should().OnlyContain(lease => lease != null);
    }

    [Fact]
    public void TryAcquire_ShouldRefuse_WhenUserIsAtTheLimit()
    {
        AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser);

        var extra = Limiter.TryAcquire(UserId);

        extra.Should().BeNull();
    }

    [Fact]
    public void TryAcquire_ShouldGrantAgain_WhenAnEarlierLeaseIsDisposed()
    {
        var leases = AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser);

        leases[0]!.Dispose();

        var replacement = Limiter.TryAcquire(UserId);

        replacement.Should().NotBeNull();
    }

    [Fact]
    public void TryAcquire_ShouldNotCountOtherUsers_WhenOneUserIsAtTheLimit()
    {
        AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser);

        var other = Limiter.TryAcquire("user-2");

        other.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldReleaseOnce_WhenCalledRepeatedly()
    {
        var leases = AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser);

        leases[0]!.Dispose();
        leases[0]!.Dispose();

        Limiter.TryAcquire(UserId).Should().NotBeNull();
        Limiter.TryAcquire(UserId).Should().BeNull();
    }

    [Fact]
    public async Task TryAcquire_ShouldNeverExceedTheLimit_WhenManyConnectionsRaceTheSameUser()
    {
        const int attempts = 200;

        var granted = await Task.WhenAll(Enumerable
            .Range(0, attempts)
            .Select(_ => Task.Run(() => Limiter.TryAcquire(UserId))));

        granted.Count(lease => lease is not null).Should().Be(SseConnectionLimiter.MaxConnectionsPerUser);
    }

    [Fact]
    public async Task Release_ShouldReturnEveryPermit_WhenManyLeasesAreDisposedConcurrently()
    {
        var leases = AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser);

        await Task.WhenAll(leases.Select(lease => Task.Run(() => lease!.Dispose())));

        AcquireMany(UserId, SseConnectionLimiter.MaxConnectionsPerUser)
            .Should()
            .OnlyContain(lease => lease != null);
    }

    private List<SseConnectionLease?> AcquireMany(string userId, int count)
    {
        return [.. Enumerable.Range(0, count).Select(_ => Limiter.TryAcquire(userId))];
    }
}
