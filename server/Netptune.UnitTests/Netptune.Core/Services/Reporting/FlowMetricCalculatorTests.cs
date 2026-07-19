using FluentAssertions;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Services.Reporting;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Services.Reporting;

public sealed class FlowMetricCalculatorTests
{
    [Fact]
    public void Calculate_ShouldCountDirectToDoneForThroughput_ButExcludeItFromCycleTime()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var facts = new[]
        {
            new FlowFact("1", start, FlowFactType.Created),
            new FlowFact("1", start, FlowFactType.Done),
            new FlowFact("1", start.AddHours(7), FlowFactType.Done),
        };

        var input = new FlowCalculationInput
        {
            Facts = facts,
            TimeZone = TimeZoneInfo.Utc,
            From = start,
            To = start.AddDays(1),
            CoverageStart = start,
        };
        var result = FlowMetricCalculator.Calculate(input);

        result.Throughput.Should().Be(1);
        result.CycleTimeSampleSize.Should().Be(0);
        result.MedianCycleTimeHours.Should().BeNull();
    }

    [Fact]
    public void Calculate_ShouldReturnMedianAndP85_WithSampleSize()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var facts = new[]
        {
            new FlowFact("1", start, FlowFactType.Active),
            new FlowFact("1", start.AddHours(2), FlowFactType.Done),
            new FlowFact("2", start, FlowFactType.Active),
            new FlowFact("2", start.AddHours(10), FlowFactType.Done),
        };

        var input = new FlowCalculationInput
        {
            Facts = facts,
            TimeZone = TimeZoneInfo.Utc,
            From = start.AddDays(-1),
            To = start.AddDays(1),
            CoverageStart = start,
        };
        var result = FlowMetricCalculator.Calculate(input);

        result.CycleTimeSampleSize.Should().Be(2);
        result.MedianCycleTimeHours.Should().Be(6);
        result.P85CycleTimeHours.Should().Be(8.8m);
        result.Coverage.IsPartial.Should().BeTrue();
    }

    [Fact]
    public void Calculate_ShouldNotCountARecompletion_WhenTheFirstDoneWasBeforeTheRange()
    {
        var start = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var facts = new[]
        {
            new FlowFact("1", start, FlowFactType.Active),
            new FlowFact("1", start.AddHours(2), FlowFactType.Done),
            new FlowFact("1", start.AddDays(2), FlowFactType.Done),
        };

        var input = new FlowCalculationInput
        {
            Facts = facts,
            TimeZone = TimeZoneInfo.Utc,
            From = start.AddDays(1),
            To = start.AddDays(3),
            CoverageStart = start,
        };
        var result = FlowMetricCalculator.Calculate(input);

        result.Throughput.Should().Be(0);
        result.CycleTimeSampleSize.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldBucketThroughputByWeek_AndCycleTimeWithPercentiles()
    {
        var monday = new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc);
        var facts = new[]
        {
            new FlowFact("1", monday, FlowFactType.Active),
            new FlowFact("1", monday.AddHours(2), FlowFactType.Done),
            new FlowFact("2", monday.AddDays(1), FlowFactType.Active),
            new FlowFact("2", monday.AddDays(1).AddHours(10), FlowFactType.Done),
        };

        var input = new FlowCalculationInput
        {
            Facts = facts,
            TimeZone = TimeZoneInfo.Utc,
            From = monday,
            To = monday.AddDays(7),
            CoverageStart = monday,
            Grouping = ReportingGrouping.Week,
        };
        var result = FlowMetricCalculator.Calculate(input);

        result.Buckets.Should().ContainSingle();
        result.Buckets.Single().Date.Should().Be(new DateOnly(2026, 7, 6));
        result.Buckets.Single().Completed.Should().Be(2);
        result.CycleTimeBuckets.Should().ContainSingle();
        result.CycleTimeBuckets.Single().MedianCycleTimeHours.Should().Be(6);
        result.CycleTimeBuckets.Single().P85CycleTimeHours.Should().Be(8.8m);
        result.CycleTimeBuckets.Single().SampleSize.Should().Be(2);
    }
}
