using System.Net;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Extensions;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;

namespace Netptune.Storage;

public class S3StorageService : IStorageService
{
    private const int MaxKeysPerDeleteRequest = 1000;

    private readonly ILogger<S3StorageService> Logger;
    private readonly IAmazonS3 S3Client;
    private readonly S3StorageOptions Options;
    private readonly RegionEndpoint Region;
    private readonly CloudFrontUrls? CloudFrontUrls;

    public S3StorageService(IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger)
        : this(options, logger, CreateS3Client(options.Value))
    {
    }

    public S3StorageService(IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger, IAmazonS3 s3Client)
    {
        Logger = logger;
        Options = options.Value;
        Region = RegionEndpoint.GetBySystemName(Options.Region);
        CloudFrontUrls = Options.CloudFront.IsEnabled ? new CloudFrontUrls(Options.CloudFront) : null;
        S3Client = s3Client;
    }

    private static AmazonS3Client CreateS3Client(S3StorageOptions options)
    {
        var region = RegionEndpoint.GetBySystemName(options.Region);

        return new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, region);
    }

    public async Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, StorageUploadOptions uploadOptions, CancellationToken cancellationToken = default)
    {
        var fileTransferUtility = new TransferUtility(S3Client);
        var bucketName = ResolveBucketName(uploadOptions.Bucket);

        var request = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            BucketName = bucketName,
            AutoCloseStream = true,
            AutoResetStreamPosition = true,
            Key = uploadOptions.Key,
            ContentType = uploadOptions.ContentType,
            CannedACL = ResolveCannedAcl(uploadOptions.Access),
        };

        try
        {
            await fileTransferUtility.UploadAsync(request, cancellationToken);

            var metadata = await S3Client.GetObjectMetadataAsync(bucketName, uploadOptions.Key, cancellationToken);

            var isServedByCloudFront = uploadOptions.Bucket == StorageBucket.Uploads && CloudFrontUrls is not null;
            var uri = isServedByCloudFront
                ? CloudFrontUrls!.GetPublicUrl(uploadOptions.Key)
                : $"https://{bucketName}.s3.{Region.SystemName}.{Region.PartitionDnsSuffix}/{uploadOptions.Key}";

            var upload = new UploadResponse
            {
                Name = uploadOptions.Name,
                Key = uploadOptions.Key,
                Path = uploadOptions.Key,
                Uri = uri,
                Size = metadata.ContentLength,
            };

            return ClientResponse<UploadResponse>.Success(upload);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Storage Service upload failed");

            return ClientResponse<UploadResponse>.Failed();
        }
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await S3Client.GetObjectAsync(Options.BucketName, key, cancellationToken);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<Uri?> GetReadUriAsync(StorageReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contentDisposition = BuildContentDisposition(readOptions);
        var expiresOn = DateTime.UtcNow.Add(readOptions.Lifetime);
        var uri = CloudFrontUrls?.GetSignedUri(readOptions.Key, contentDisposition, expiresOn)
            ?? GetPresignedUri(readOptions.Key, contentDisposition, expiresOn);

        return Task.FromResult<Uri?>(uri);
    }

    private static string BuildContentDisposition(StorageReadOptions readOptions)
    {
        var dispositionType = readOptions.Disposition == StorageDisposition.Inline ? "inline" : "attachment";
        var safeName = readOptions.FileName.SanitizeFileName();
        var asciiName = new string(safeName.Select(character => char.IsAscii(character) ? character : '_').ToArray());
        var encodedName = Uri.EscapeDataString(safeName);

        // CloudFront rewrites non-ASCII header bytes to '?', so the real name has to travel percent-encoded.
        return $"{dispositionType}; filename=\"{asciiName}\"; filename*=UTF-8''{encodedName}";
    }

    private Uri GetPresignedUri(string key, string contentDisposition, DateTime expiresOn)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Options.BucketName,
            Key = key,
            Expires = expiresOn,
            Verb = HttpVerb.GET,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = contentDisposition,
            },
        };

        return new Uri(S3Client.GetPreSignedURL(request));
    }

    private string ResolveBucketName(StorageBucket bucket)
    {
        var isAuditArchive = bucket == StorageBucket.AuditArchive;
        var hasAuditArchiveBucket = !string.IsNullOrWhiteSpace(Options.AuditArchiveBucketName);

        return isAuditArchive && hasAuditArchiveBucket ? Options.AuditArchiveBucketName! : Options.BucketName;
    }

    private S3CannedACL? ResolveCannedAcl(StorageAccess access)
    {
        // A bucket that blocks public access or disables ACLs rejects uploads that send one, even "private", which
        // is already what an object gets without an ACL.
        var needsPublicAcl = access == StorageAccess.PublicRead && CloudFrontUrls is null;

        return needsPublicAcl ? S3CannedACL.PublicRead : null;
    }

    public async Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        await S3Client.DeleteObjectAsync(Options.BucketName, key, cancellationToken);
    }

    public async Task DeleteFilesAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        var requests = keys
            .Chunk(MaxKeysPerDeleteRequest)
            .Select(chunk => DeleteObjects(chunk, cancellationToken));

        await Task.WhenAll(requests);
    }

    private async Task DeleteObjects(string[] keys, CancellationToken cancellationToken)
    {
        var request = new DeleteObjectsRequest
        {
            BucketName = Options.BucketName,
            Objects = keys.Select(key => new KeyVersion { Key = key }).ToList(),
            Quiet = true,
        };

        await S3Client.DeleteObjectsAsync(request, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await S3Client.GetObjectMetadataAsync(Options.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
