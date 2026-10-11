namespace Netptune.Core.Models.Pages;

public static class PageTitle
{
    public const int MaxLength = 256;

    public const string Untitled = "Untitled";

    public const string TooLongMessage = "Page titles can be at most 256 characters.";

    public static string Normalise(string? title)
    {
        var trimmed = title?.Trim();
        var isBlank = string.IsNullOrEmpty(trimmed);

        return isBlank ? Untitled : trimmed!;
    }

    public static bool IsTooLong(string title) => title.Length > MaxLength;
}
