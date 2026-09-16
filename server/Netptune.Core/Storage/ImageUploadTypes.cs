using System.Collections.Frozen;

namespace Netptune.Core.Storage;

public static class ImageUploadTypes
{
    public static IReadOnlySet<string> Allowed { get; } = new[]
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
        "image/avif",
    }.ToFrozenSet();

    private static readonly Dictionary<string, string> ExtensionsByContentType = new()
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
        ["image/avif"] = ".avif",
    };

    public static bool IsAllowed(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var normalised = Normalize(contentType);

        return Allowed.Contains(normalised);
    }

    public static string Normalize(string contentType)
    {
        var separatorIndex = contentType.IndexOf(';');
        var withoutParameters = separatorIndex < 0 ? contentType : contentType[..separatorIndex];

        return withoutParameters.Trim().ToLowerInvariant();
    }

    // Taken from the validated content type rather than the supplied filename, so a caller cannot
    // choose the extension the object is stored and later served under.
    public static string ExtensionFor(string contentType)
    {
        var normalised = Normalize(contentType);

        return ExtensionsByContentType.GetValueOrDefault(normalised, ".bin");
    }

    // Serving these inline is safe: none of them are document formats a browser will run script from.
    public static bool IsInlineSafe(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var normalised = Normalize(contentType);
        var isInlineSafeImage = Allowed.Contains(normalised);
        var isPdf = normalised == "application/pdf";

        return isInlineSafeImage || isPdf;
    }

    public static string Describe()
    {
        return "PNG, JPEG, WebP, GIF or AVIF";
    }
}
