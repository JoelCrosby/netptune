using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;

namespace Netptune.IntegrationTests.TestServices;

public class TestStorageService : IStorageService
{
    private readonly Dictionary<string, byte[]> Objects = new();

    public Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, StorageUploadOptions uploadOptions, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();

        stream.CopyTo(memory);

        Objects[uploadOptions.Key] = memory.ToArray();

        var result = ClientResponse<UploadResponse>.Success(new UploadResponse
        {
            Name = uploadOptions.Name,
            Key = uploadOptions.Key,
            Path = uploadOptions.Key,
            Size = Objects[uploadOptions.Key].LongLength,
            Uri = $"https://bucket.s3.region.suffix/{uploadOptions.Key}",
        });

        return Task.FromResult(result);
    }

    public Task<Uri?> GetReadUriAsync(StorageReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Objects.ContainsKey(readOptions.Key) ? new Uri($"https://storage.test/{Uri.EscapeDataString(readOptions.Key)}") : null);
    }

    public Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        Objects.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Objects.ContainsKey(key));
    }
}
