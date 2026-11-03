using Amazon.CloudFront;

namespace Netptune.Storage;

internal sealed class CloudFrontUrls
{
    private readonly string BaseUrl;
    private readonly string KeyPairId;
    private readonly string PrivateKey;

    public CloudFrontUrls(CloudFrontOptions options)
    {
        BaseUrl = options.BaseUrl!.TrimEnd('/');
        KeyPairId = options.KeyPairId!;
        PrivateKey = options.PrivateKey!;
    }

    public string GetPublicUrl(string key)
    {
        var escapedSegments = key.Split('/').Select(Uri.EscapeDataString);

        return $"{BaseUrl}/{string.Join('/', escapedSegments)}";
    }

    public Uri GetSignedUri(string key, string contentDisposition, DateTime expiresOn)
    {
        var escapedDisposition = Uri.EscapeDataString(contentDisposition);
        var url = $"{GetPublicUrl(key)}?response-content-disposition={escapedDisposition}";

        using var privateKey = new StringReader(PrivateKey);

        var signedUrl = AmazonCloudFrontUrlSigner.GetCannedSignedURL(url, privateKey, KeyPairId, expiresOn);

        return new Uri(signedUrl);
    }
}
