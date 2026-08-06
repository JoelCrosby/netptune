using System.Globalization;

namespace Netptune.Transfer.Export;

public sealed class ExportValueFormatter
{
    private readonly ExportOptionsModel Options;
    private readonly TimeZoneInfo TimeZone;

    public ExportValueFormatter(ExportOptionsModel options)
    {
        Options = options;
        TimeZone = ResolveTimeZone(options.TimeZoneId);
    }

    public string Format(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            EntityRef entityRef => entityRef.Value,
            DateOnly date => date.ToString(Options.DateFormat, CultureInfo.InvariantCulture),
            DateTime dateTime => FormatDateTime(dateTime),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            double number => number.ToString(CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false",
            IEnumerable<EntityRef> refs => string.Join(Options.CollectionSeparator, refs.Select(item => item.Value)),
            IEnumerable<string> values => string.Join(Options.CollectionSeparator, values),
            Enum enumValue => enumValue.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    public IReadOnlyList<object?> Expand(object? value)
    {
        if (value is IEnumerable<EntityRef> refs)
        {
            var items = refs.Cast<object?>().ToList();

            return items.Count == 0 ? [null] : items;
        }

        if (value is string)
        {
            return [value];
        }

        if (value is IEnumerable<string> values)
        {
            var items = values.Cast<object?>().ToList();

            return items.Count == 0 ? [null] : items;
        }

        return [value];
    }

    public DateTime ToExportZone(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }

    private string FormatDateTime(DateTime value)
    {
        var local = ToExportZone(value);

        return local.ToString($"{Options.DateFormat} HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        var found = TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone);

        if (!found || timeZone is null)
        {
            return TimeZoneInfo.Utc;
        }

        return timeZone;
    }
}
