using FluentAssertions;

using Netptune.Core.Services.Reporting;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Services.Reporting;

public sealed class FlowMetricCalculatorTests
{
    [Fact]
    public void Calculate_ShouldDeduplicateRepeatedDoneTransitions()
    {
        var start = new DateTime(
            2026,
            7,
            1,
            9,
            0,
            0,
            DateTimeKind.Utc);
        var facts = new[]
        {
            new FlowFact("1", start, FlowFactType.Created),
            new FlowFact("1", start.AddHours(3), FlowFactType.Done),
            new FlowFact("1", start.AddHours(7), FlowFactType.Done),
        };

        var result = FlowMetricCalculator.Calculate(
            facts,
            TimeZoneInfo.Utc,
            start,
            start.AddDays(1),
            start);

        result.Throughput.Should().Be(1);
        result.CycleTimeSampleSize.Should().Be(1);
        result.MedianCycleTimeHours.Should().Be(3);
    }

    [Fact]
    public void Calculate_ShouldReturnMedianAndP85_WithSampleSize()
    {
        var start = new DateTime(
            2026,
            7,
            1,
            9,
            0,
            0,
            DateTimeKind.Utc);
        var facts = new[]
        {
            new FlowFact("1", start, FlowFactType.Active),
            new FlowFact("1", start.AddHours(2), FlowFactType.Done),
            new FlowFact("2", start, FlowFactType.Active),
            new FlowFact("2", start.AddHours(10), FlowFactType.Done),
        };

        var result = FlowMetricCalculator.Calculate(
            facts,
            TimeZoneInfo.Utc,
            start.AddDays(-1),
            start.AddDays(1),
            start);

        result.CycleTimeSampleSize.Should().Be(2);
        result.MedianCycleTimeHours.Should().Be(6);
        result.P85CycleTimeHours.Should().Be(8.8m);
        result.Coverage.IsPartial.Should().BeTrue();
    }
}
