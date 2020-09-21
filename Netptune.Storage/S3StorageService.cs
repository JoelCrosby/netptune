using System;
using System.IO;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;

namespace Netptune.Storage
{
    public class S3StorageService : IStorageService
    {
        private readonly ILogger<S3StorageService> Logger;
        private readonly IAmazonS3 S3Client;
        private readonly S3StorageOptions Options;

        public S3StorageService(IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger)
        {
            Logger = logger;
            Options = options.Value;

            S3Client = new AmazonS3Client(
                Options.AccessKeyID,
                Options.SecretAccessKey,
                RegionEndpoint.EUWest2
            );
        }

        public async Task<ClientResponse> UploadFileAsync(Stream stream, string key = null)
        {
            var fileTransferUtility = new TransferUtility(S3Client);

            var request = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                BucketName = Options.BucketName,
                AutoCloseStream = true,
                AutoResetStreamPosition = true,
                Key = key,
            };

            try
            {
                await fileTransferUtility.UploadAsync(request);

                return ClientResponse.Success();
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);

                return ClientResponse.Failed();
            }
        }
    }
}
