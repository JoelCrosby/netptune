using System.Globalization;

namespace Netptune.Core.Services.Ai;

public static class AiChangeFields
{
    private const string DateFormat = "yyyy-MM-dd";

    public static AiChangeField Text(string name, string? before, string? after)
    {
        return new AiChangeField { Name = name, Before = before, After = after };
    }

    public static AiChangeField Date(string name, DateOnly? before, DateOnly? after)
    {
        return Values(
            name,
            AiChangeValueKind.Date,
            ToDateValues(before),
            ToDateValues(after));
    }

    public static AiChangeField Date(string name, DateTime? before, DateTime? after)
    {
        return Date(
            name,
            before.HasValue ? DateOnly.FromDateTime(before.Value) : null,
            after.HasValue ? DateOnly.FromDateTime(after.Value) : null);
    }

    public static AiChangeField Values(
        string name,
        AiChangeValueKind kind,
        IEnumerable<AiChangeValue>? before,
        IEnumerable<AiChangeValue>? after)
    {
        var beforeValues = before?.ToList() ?? [];
        var afterValues = after?.ToList() ?? [];

        return new AiChangeField
        {
            Name = name,
            Kind = kind,
            BeforeValues = beforeValues,
            AfterValues = afterValues,
            Before = Render(beforeValues),
            After = Render(afterValues),
        };
    }

    public static AiChangeValue User(string? id, string displayName, string? pictureUrl = null)
    {
        return new AiChangeValue { Id = id, Display = displayName, PictureUrl = pictureUrl };
    }

    public static AiChangeValue Status(int? id, string name, string? color = null)
    {
        return new AiChangeValue { Id = id?.ToString(CultureInfo.InvariantCulture), Display = name, Color = color };
    }

    public static AiChangeValue Tag(string name)
    {
        return new AiChangeValue { Display = name };
    }

    public static AiChangeValue Task(int? id, string? systemId, string name)
    {
        var display = string.IsNullOrWhiteSpace(systemId) ? name : $"{systemId} · {name}";

        return new AiChangeValue { Id = id?.ToString(CultureInfo.InvariantCulture), Display = display };
    }

    public static AiChangeValue Sprint(int? id, string name)
    {
        return new AiChangeValue { Id = id?.ToString(CultureInfo.InvariantCulture), Display = name };
    }

    private static List<AiChangeValue> ToDateValues(DateOnly? date)
    {
        if (!date.HasValue)
        {
            return [];
        }

        return [new AiChangeValue { Display = date.Value.ToString(DateFormat, CultureInfo.InvariantCulture) }];
    }

    private static string? Render(List<AiChangeValue> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return string.Join(", ", values.Select(value => value.Display));
    }
}
