using System.Security.Cryptography;

namespace Netptune.Core.Models.Activity;

public static class ActivityValue
{
    public const int MaxLength = 256;

    public static string? Truncate(string? value)
    {
        if (value is null || value.Length <= MaxLength) return value;

        return value[..MaxLength];
    }

    public static string? HashIfTruncated(string? value)
    {
        if (value is null || value.Length <= MaxLength) return null;

        return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    }
}
