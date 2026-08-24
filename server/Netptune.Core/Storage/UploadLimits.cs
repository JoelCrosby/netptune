namespace Netptune.Core.Storage;

public static class UploadLimits
{
    public const long DefaultMaxUploadBytes = 50L * 1024 * 1024;

    public const long MinimumMaxUploadBytes = 1L * 1024 * 1024;

    public const long MaximumMaxUploadBytes = 512L * 1024 * 1024;

    public const long ProfilePictureMaxBytes = DefaultMaxUploadBytes;

    public const long BrandingImageMaxBytes = 10L * 1024 * 1024;

    public const long RequestOverheadBytes = 1024 * 1024;

    public const long MaximumRequestBytes = MaximumMaxUploadBytes + RequestOverheadBytes;

    public static long Clamp(long value)
    {
        return Math.Clamp(value, MinimumMaxUploadBytes, MaximumMaxUploadBytes);
    }

    public static string Describe(long bytes)
    {
        var megabytes = bytes / 1024d / 1024d;

        return $"{megabytes:0.#} MiB";
    }
}
