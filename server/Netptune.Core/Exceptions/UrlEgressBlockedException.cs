using System.Diagnostics.CodeAnalysis;

namespace Netptune.Core.Exceptions;

public sealed class UrlEgressBlockedException : Exception
{
    public UrlEgressBlockedException(string message)
        : base(message)
    {
    }

    public UrlEgressBlockedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [DoesNotReturn]
    public static void Throw(string message)
    {
        throw new UrlEgressBlockedException(message);
    }

    [DoesNotReturn]
    public static void Throw(string message, Exception innerException)
    {
        throw new UrlEgressBlockedException(message, innerException);
    }

    [DoesNotReturn]
    public static T Throw<T>(string message)
    {
        throw new UrlEgressBlockedException(message);
    }

    [DoesNotReturn]
    public static T Throw<T>(string message, Exception innerException)
    {
        throw new UrlEgressBlockedException(message, innerException);
    }

    public static Uri ThrowIfNotAbsoluteUrl([NotNull] string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            Throw("The supplied url is not a valid absolute url.");
        }

        return uri;
    }

    public static void ThrowIfNotSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        Throw($"The supplied url returned a non-success status code ({(int) response.StatusCode}).");
    }
}
