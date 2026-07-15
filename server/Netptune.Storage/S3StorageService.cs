using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Extensions;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.Storage;

namespace Netptune.Storage;

public class S3StorageService : ServiceBase<UploadResponse>, IStorageService
{
    private readonly ILogger<S3StorageService> Logger;
    private readonly IAmazonS3 S3Client;
    private readonly S3StorageOptions Options;
    private readonly RegionEndpoint Region;

    public S3StorageService(IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger)
    {
        Logger = logger;
        Options = options.Value;
        Region = RegionEndpoint.GetBySystemName(Options.Region);

        S3Client = new AmazonS3Client(
            Options.AccessKeyID,
            Options.SecretAccessKey,
            Region
        );
    }

    public async Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, StorageUploadOptions uploadOptions, CancellationToken cancellationToken = default)
    {
        var fileTransferUtility = new TransferUtility(S3Client);

        var request = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            BucketName = Options.BucketName,
            AutoCloseStream = true,
            AutoResetStreamPosition = true,
            Key = uploadOptions.Key,
            ContentType = uploadOptions.ContentType,
            CannedACL = uploadOptions.Access == StorageAccess.PublicRead ? S3CannedACL.PublicRead : S3CannedACL.Private,
        };

        try
        {
            await fileTransferUtility.UploadAsync(request, cancellationToken);

            var metadata = await S3Client.GetObjectMetadataAsync(Options.BucketName, uploadOptions.Key, cancellationToken);

            var uri = $"https://{Options.BucketName}.s3.{Region.SystemName}.{Region.PartitionDnsSuffix}/{uploadOptions.Key}";

            return Success(new UploadResponse
            {
                Name = uploadOptions.Name,
                Key = uploadOptions.Key,
                Path = uploadOptions.Key,
                Uri = uri,
                Size = metadata.ContentLength,
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Storage Service upload failed");

            return Failed();
        }
    }

    public Task<Uri?> GetReadUriAsync(StorageReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeName = readOptions.FileName.SanitizeFileName();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = Options.BucketName,
            Key = readOptions.Key,
            Expires = DateTime.UtcNow.Add(readOptions.Lifetime),
            Verb = HttpVerb.GET,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"{(readOptions.Disposition == StorageDisposition.Inline ? "inline" : "attachment")}; filename=\"{safeName}\"",
            },
        };

        return Task.FromResult<Uri?>(new Uri(S3Client.GetPreSignedURL(request)));
    }

    public async Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        await S3Client.DeleteObjectAsync(Options.BucketName, key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await S3Client.GetObjectMetadataAsync(Options.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
