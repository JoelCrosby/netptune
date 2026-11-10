using System.Text.RegularExpressions;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal static partial class AzureDevOpsOrganisation
{
    private const string DevAzureHost = "dev.azure.com";
    private const string VisualStudioHostSuffix = ".visualstudio.com";

    public static string? Parse(string? input)
    {
        var trimmed = input?.Trim().TrimEnd('/');

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (IsName(trimmed))
        {
            return trimmed;
        }

        var hasScheme = trimmed.Contains("://", StringComparison.Ordinal);
        var address = hasScheme ? trimmed : $"https://{trimmed}";
        var isAddress = Uri.TryCreate(address, UriKind.Absolute, out var uri);

        if (!isAddress)
        {
            return null;
        }

        var isWebAddress = uri!.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;

        if (!isWebAddress)
        {
            return null;
        }

        var name = ReadName(uri);
        var isValidName = name is not null && IsName(name);

        return isValidName ? name : null;
    }

    private static string? ReadName(Uri uri)
    {
        var host = uri.Host;

        if (host == DevAzureHost)
        {
            var firstSegment = uri.Segments.Skip(1).FirstOrDefault()?.TrimEnd('/');

            return firstSegment is null ? null : Uri.UnescapeDataString(firstSegment);
        }

        var isVisualStudioHost = host.EndsWith(VisualStudioHostSuffix, StringComparison.Ordinal);

        return isVisualStudioHost ? host[..^VisualStudioHostSuffix.Length] : null;
    }

    private static bool IsName(string value)
    {
        return NamePattern().IsMatch(value);
    }

    [GeneratedRegex("^[A-Za-z0-9](?:[A-Za-z0-9-]{0,48}[A-Za-z0-9])?$")]
    private static partial Regex NamePattern();
}
