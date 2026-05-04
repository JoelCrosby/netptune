using System.Globalization;

namespace Netptune.Core.Requests;

public sealed class CursorRequest
{
    public string? Cursor { get; init; }

    public int? Take { get; init; }

    public int GetTake(int maxTake = PaginationDefaults.MaxPageSize)
    {
        return Math.Clamp(Take ?? PaginationDefaults.DefaultPageSize, 1, maxTake);
    }

    public static string Create(DateTime value, int id)
    {
        return $"{value.Ticks}:{id}";
    }

    public static string Create(DateTimeOffset value, int id)
    {
        return $"{value.UtcTicks}:{id}";
    }

    public bool TryGetCursor(out DateTime value, out int id)
    {
        value = default;
        id = default;

        if (string.IsNullOrWhiteSpace(Cursor)) return false;

        var parts = Cursor.Split(':', 2);

        if (parts.Length != 2) return false;

        if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
        {
            return false;
        }

        value = new DateTime(ticks, DateTimeKind.Utc);

        return true;
    }
}
