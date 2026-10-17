using System.Security.Cryptography;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubAppOptions
{
    public const string SectionName = "SourceControl:Providers:GitHub";

    public bool Enabled { get; set; }

    public string AppId { get; set; } = string.Empty;

    public string AppSlug { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string ApiBaseUrl { get; set; } = "https://api.github.com";

    public string WebBaseUrl { get; set; } = "https://github.com";

    // Environment variables often carry the PEM with escaped newlines.
    public string PrivateKeyPem => PrivateKey.Replace("\\n", "\n");

    public bool HasReadablePrivateKey()
    {
        using var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(PrivateKeyPem);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public bool IsComplete()
    {
        string[] required = [AppId, AppSlug, ClientId, ClientSecret, PrivateKey, WebhookSecret, ApiBaseUrl, WebBaseUrl];

        return required.All(value => !string.IsNullOrWhiteSpace(value));
    }
}
