using System.Collections.Frozen;

namespace Netptune.Core.Storage;

public static class BrandingImageTypes
{
    public static IReadOnlySet<string> Allowed { get; } = new[]
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
        "image/avif",
    }.ToFrozenSet();

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

    public static string Describe()
    {
        return "PNG, JPEG, WebP, GIF or AVIF";
    }
}
