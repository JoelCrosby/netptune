using Netptune.Core.Models.Reporting;

namespace Netptune.Core.Services.Reporting;

public enum FlowFactType
{
    Created,
    Active,
    Done,
}

public sealed record FlowFact(string TaskId, DateTime OccurredAt, FlowFactType Type);

public sealed record FlowCalculationInput
{
    public required IReadOnlyCollection<FlowFact> Facts { get; init; }

    public required TimeZoneInfo TimeZone { get; init; }

    public DateTime From { get; init; }

    public DateTime To { get; init; }

    public DateTime? CoverageStart { get; init; }

    public ReportingGrouping Grouping { get; init; } = ReportingGrouping.Day;
}

public static class FlowMetricCalculator
{
    public static FlowReport Calculate(FlowCalculationInput input)
    {
        var taskFacts = input.Facts
            .GroupBy(fact => fact.TaskId)
            .ToDictionary(group => group.Key, group => group.OrderBy(fact => fact.OccurredAt).ToList());
        var firstCompletions = new Dictionary<string, DateTime>();
        var cycleSamples = new List<CycleSample>();

        foreach (var (taskId, orderedFacts) in taskFacts)
        {
            var createdAt = orderedFacts.FirstOrDefault(fact => fact.Type == FlowFactType.Created)?.OccurredAt;
            var firstDone = orderedFacts.FirstOrDefault(fact => fact.Type == FlowFactType.Done)?.OccurredAt;

            if (firstDone.HasValue)
            {
                firstCompletions[taskId] = firstDone.Value;
            }

            var firstActive = orderedFacts.FirstOrDefault(fact => fact.Type == FlowFactType.Active)?.OccurredAt;
            var wasCreatedInDone = createdAt.HasValue && firstDone == createdAt;
            var hasCycleStart = firstActive.HasValue;
            var hasMeasurableCycle = hasCycleStart && !wasCreatedInDone;

            if (!hasMeasurableCycle)
            {
                continue;
            }

            var activatedAt = firstActive.GetValueOrDefault();
            var completedAfterActivation = orderedFacts.FirstOrDefault(
                fact => IsCompletionAfter(fact, activatedAt))?.OccurredAt;
            var completedWithinRange = completedAfterActivation.HasValue &&
                IsWithinRange(completedAfterActivation.Value, input.From, input.To);

            if (completedWithinRange)
            {
                var completedAt = completedAfterActivation.GetValueOrDefault();

                cycleSamples.Add(new CycleSample(completedAt, (decimal)(completedAt - activatedAt).TotalHours));
            }
        }

        var completionsInRange = firstCompletions.Values
            .Where(completedAt => IsWithinRange(completedAt, input.From, input.To))
            .ToList();
        var samples = cycleSamples
            .Select(sample => sample.Hours)
            .Order()
            .ToList();

        var buckets = completionsInRange
            .GroupBy(value => BucketDate(value, input.TimeZone, input.Grouping))
            .OrderBy(group => group.Key)
            .Select(group => new FlowBucket(group.Key, group.Count()))
            .ToList();

        var cycleTimeBuckets = cycleSamples
            .GroupBy(sample => BucketDate(sample.CompletedAt, input.TimeZone, ReportingGrouping.Week))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var bucketSamples = group.Select(sample => sample.Hours).Order().ToList();

                return new CycleTimeBucket
                {
                    WeekStarting = group.Key,
                    MedianCycleTimeHours = Percentile(bucketSamples, .5m),
                    P85CycleTimeHours = Percentile(bucketSamples, .85m),
                    SampleSize = bucketSamples.Count,
                };
            })
            .ToList();

        var isPartialCoverage = input.CoverageStart is null || input.CoverageStart > input.From;

        return new FlowReport
        {
            Buckets = buckets,
            Throughput = completionsInRange.Count,
            MedianCycleTimeHours = Percentile(samples, .5m),
            P85CycleTimeHours = Percentile(samples, .85m),
            CycleTimeSampleSize = samples.Count,
            CycleTimeBuckets = cycleTimeBuckets,
            Coverage = new ReportingCoverage(input.CoverageStart, isPartialCoverage),
        };
    }

    private static bool IsCompletionAfter(FlowFact fact, DateTime activeAt)
    {
        var isCompletion = fact.Type == FlowFactType.Done;
        var occurredAfterActivation = fact.OccurredAt >= activeAt;

        return isCompletion && occurredAfterActivation;
    }

    private static bool IsWithinRange(DateTime value, DateTime from, DateTime to)
    {
        var isOnOrAfterStart = value >= from;
        var isOnOrBeforeEnd = value <= to;

        return isOnOrAfterStart && isOnOrBeforeEnd;
    }

    private static DateOnly BucketDate(
        DateTime value,
        TimeZoneInfo timeZone,
        ReportingGrouping grouping)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(value, DateTimeKind.Utc),
            timeZone));

        if (grouping == ReportingGrouping.Day)
        {
            return localDate;
        }

        var daysSinceMonday = ((int)localDate.DayOfWeek + 6) % 7;

        return localDate.AddDays(-daysSinceMonday);
    }

    private static decimal? Percentile(IReadOnlyList<decimal> values, decimal percentile)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var index = (values.Count - 1) * percentile;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        return values[lower] + (values[upper] - values[lower]) * (index - lower);
    }

    private sealed record CycleSample(DateTime CompletedAt, decimal Hours);
}
