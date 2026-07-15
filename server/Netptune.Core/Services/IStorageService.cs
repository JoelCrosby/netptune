using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Storage;

namespace Netptune.Core.Services;

public interface IStorageService
{
    Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, StorageUploadOptions uploadOptions, CancellationToken cancellationToken = default);

    Task<Uri?> GetReadUriAsync(StorageReadOptions readOptions, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
