using FluentAssertions;

using Netptune.Core.Utilities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Utilities;

public class WorkspaceTimeTests
{
    private static readonly TimeZoneInfo Sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");
    private static readonly TimeZoneInfo Santiago = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

    [Fact]
    public void ToLocalDate_UsesTheWorkspaceDayNotTheUtcOne()
    {
        var lateUtc = new DateTime(2026, 8, 20, 22, 0, 0, DateTimeKind.Utc);

        WorkspaceTime.ToLocalDate(lateUtc, Sydney).Should().Be(new DateOnly(2026, 8, 21));
        WorkspaceTime.ToLocalDate(lateUtc, TimeZoneInfo.Utc).Should().Be(new DateOnly(2026, 8, 20));
    }

    [Fact]
    public void StartOfDayUtc_ResolvesMidnightInTheWorkspaceZone()
    {
        var start = WorkspaceTime.StartOfDayUtc(new DateOnly(2026, 8, 21), Sydney);

        start.Should().Be(new DateTime(2026, 8, 20, 14, 0, 0, DateTimeKind.Utc));
    }

    // Santiago springs forward at midnight, so 00:00 does not exist on this date and ConvertTimeToUtc
    // rejects it outright. A view filtering on created_at must still resolve the day.
    [Fact]
    public void StartOfDayUtc_UsesTheFirstRealMinute_WhenTheClockJumpsAtMidnight()
    {
        var resolve = () => WorkspaceTime.StartOfDayUtc(new DateOnly(2018, 8, 12), Santiago);

        resolve.Should().NotThrow();
        resolve().Should().Be(new DateTime(2018, 8, 12, 4, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void StartOfDayUtc_RoundTripsWithToLocalDate()
    {
        var date = new DateOnly(2018, 8, 12);
        var start = WorkspaceTime.StartOfDayUtc(date, Santiago);

        WorkspaceTime.ToLocalDate(start, Santiago).Should().Be(date);
    }
}
