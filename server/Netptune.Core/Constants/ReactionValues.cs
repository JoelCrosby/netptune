namespace Netptune.Core.Constants;

public static class ReactionValues
{
    public const string ThumbsUp = "👍";

    public const string ThumbsDown = "👎";

    public const string Laugh = "😄";

    public const string Celebrate = "🎉";

    public const string Confused = "😕";

    public const string Heart = "❤️";

    public const string Rocket = "🚀";

    public const string Eyes = "👀";

    public static readonly IReadOnlyList<string> All =
    [
        ThumbsUp,
        ThumbsDown,
        Laugh,
        Celebrate,
        Confused,
        Heart,
        Rocket,
        Eyes,
    ];

    private static readonly HashSet<string> Allowed = [.. All];

    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        var isSupported = trimmed is not null && Allowed.Contains(trimmed);

        if (!isSupported)
        {
            return null;
        }

        return trimmed;
    }
}
