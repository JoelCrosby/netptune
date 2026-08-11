namespace Netptune.Core.Storage;

public static class UploadLimits
{
    public const long DefaultMaxUploadBytes = 50L * 1024 * 1024;

    public const long MinimumMaxUploadBytes = 1L * 1024 * 1024;

    public const long MaximumMaxUploadBytes = 512L * 1024 * 1024;

    // A profile picture belongs to the user rather than to any one workspace, so it is not covered
    // by the per-workspace limit.
    public const long ProfilePictureMaxBytes = DefaultMaxUploadBytes;

    // Multipart boundaries and headers ride along with the file, so the transport limit has to sit
    // above the file limit or an upload of exactly the maximum size is rejected before it is read.
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
