using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;

namespace Netptune.IntegrationTests;

public class TestStorageService : IStorageService
{
    public Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, string name, string key)
    {
        var result = ClientResponse<UploadResponse>.Success(new UploadResponse
        {
            Name = name,
            Key = key,
            Path = key,
            Size = 1,
            Uri = $"https://bucket.s3.region.suffix/{key}",
        });

        return Task.FromResult(result);
    }
}
