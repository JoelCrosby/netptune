using System.Security.Cryptography;

namespace Netptune.Storage;

public class CloudFrontOptions
{
    public string? BaseUrl { get; set; }

    public string? KeyPairId { get; set; }

    public string? PrivateKey { get; set; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(BaseUrl);

    public bool HasHttpsBaseUrl
    {
        get
        {
            var isAbsolute = Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri);

            return isAbsolute && uri!.Scheme == Uri.UriSchemeHttps;
        }
    }

    public bool HasSigningKey()
    {
        var hasKeyPairId = !string.IsNullOrWhiteSpace(KeyPairId);

        return hasKeyPairId && HasReadablePrivateKey();
    }

    private bool HasReadablePrivateKey()
    {
        if (string.IsNullOrWhiteSpace(PrivateKey))
        {
            return false;
        }

        using var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(PrivateKey);

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            return false;
        }
    }
}
