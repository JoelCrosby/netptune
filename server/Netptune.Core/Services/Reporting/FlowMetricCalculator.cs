using Netptune.Core.Models.Reporting;

namespace Netptune.Core.Services.Reporting;

public enum FlowFactType
{
    Created,
    Active,
    Done,
}

public sealed record FlowFact(string TaskId, DateTime OccurredAt, FlowFactType Type);

public static class FlowMetricCalculator
{
    public static FlowReport Calculate(
        IReadOnlyCollection<FlowFact> facts,
        TimeZoneInfo timeZone,
        DateTime from,
        DateTime to,
        DateTime? coverageStart)
    {
        var completions = new Dictionary<string, DateTime>();
        var created = new Dictionary<string, DateTime>();
        var activated = new Dictionary<string, DateTime>();

        foreach (var fact in facts.OrderBy(fact => fact.OccurredAt))
        {

            if (fact.Type == FlowFactType.Created)
            {
                created.TryAdd(fact.TaskId, fact.OccurredAt);
            }

            if (fact.Type == FlowFactType.Active)
            {
                activated.TryAdd(fact.TaskId, fact.OccurredAt);
            }

            if (fact.Type == FlowFactType.Done && fact.OccurredAt >= from && fact.OccurredAt <= to)
            {
                completions.TryAdd(fact.TaskId, fact.OccurredAt);
            }
        }

        var samples = completions
            .Select(pair => new
            {
                pair.Value,
                Start = activated.GetValueOrDefault(pair.Key, created.GetValueOrDefault(pair.Key)),
            })
            .Where(pair => pair.Start != default && pair.Start <= pair.Value)
            .Select(pair => (decimal)(pair.Value - pair.Start).TotalHours)
            .Order()
            .ToList();

        var buckets = completions.Values
            .GroupBy(value => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), timeZone)))
            .OrderBy(group => group.Key)
            .Select(group => new FlowBucket(group.Key, group.Count(), null))
            .ToList();

        return new FlowReport
        {
            Buckets = buckets,
            Throughput = completions.Count,
            MedianCycleTimeHours = Percentile(samples, .5m),
            P85CycleTimeHours = Percentile(samples, .85m),
            CycleTimeSampleSize = samples.Count,
            Coverage = new ReportingCoverage(coverageStart, coverageStart is null || coverageStart > from),
        };
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
}
