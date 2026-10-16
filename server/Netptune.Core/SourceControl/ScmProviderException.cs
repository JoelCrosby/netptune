namespace Netptune.Core.SourceControl;

public sealed class ScmProviderException : Exception
{
    public SourceControlProvider Provider { get; }

    public int StatusCode { get; }

    public ScmProviderException(SourceControlProvider provider, int statusCode, string? providerMessage)
        : base($"{provider} returned {statusCode}: {providerMessage ?? "no message"}")
    {
        Provider = provider;
        StatusCode = statusCode;
    }
}
