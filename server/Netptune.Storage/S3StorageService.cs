using System;
using System.IO;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;

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

    public async Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, string name, string key = null)
    {
        var fileTransferUtility = new TransferUtility(S3Client);

        var request = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            BucketName = Options.BucketName,
            AutoCloseStream = true,
            AutoResetStreamPosition = true,
            Key = key,
            CannedACL = S3CannedACL.PublicRead,
        };

        try
        {
            await fileTransferUtility.UploadAsync(request);

            var uri = $"https://{Options.BucketName}.s3.{Region.SystemName}.{Region.PartitionDnsSuffix}/{key}";

            return Success(new UploadResponse
            {
                Name = name,
                Key = key,
                Path = key,
                Uri = uri,
                Size = stream.Length,
            });
        }
        catch (Exception e)
        {
            Logger.LogError("{Exception}", e.Message);

            return Failed();
        }
    }
}
