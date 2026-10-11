namespace Netptune.Core.Models.Pages;

public static class PageBody
{
    public const int MaxBytes = 1024 * 1024;

    public const string TooLargeMessage = "Pages can be at most 1 MB of text.";

    public static bool IsTooLarge(string? body)
    {
        if (body is null)
        {
            return false;
        }

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(body);

        return byteCount > MaxBytes;
    }
}
