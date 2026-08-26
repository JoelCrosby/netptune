using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Services;

namespace Netptune.Import;

public sealed class ImportSourceStore : IImportSourceStore
{
    private readonly IStorageService Storage;

    public ImportSourceStore(IStorageService storage)
    {
        Storage = storage;
    }

    public async Task<string> Save(
        string workspaceSlug,
        Guid publicId,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var storageKey = $"{PathConstants.ImportPath(workspaceSlug)}{publicId:N}/{fileName}";
        var uploadOptions = new StorageUploadOptions
        {
            Name = fileName,
            Key = storageKey,
            ContentType = contentType,
            Access = StorageAccess.Private,
        };
        var response = await Storage.UploadFileAsync(content, uploadOptions, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException("The import file could not be stored.");
        }

        return storageKey;
    }

    public async Task<Stream> Open(ImportSession session, CancellationToken cancellationToken = default)
    {
        var source = await Storage.OpenReadAsync(session.StorageKey, cancellationToken)
            ?? throw new InvalidOperationException("The import file could not be read.");
        var buffer = new MemoryStream();

        await using (source)
        {
            await source.CopyToAsync(buffer, cancellationToken);
        }

        buffer.Seek(0, SeekOrigin.Begin);

        return buffer;
    }

    public Task Delete(string storageKey, CancellationToken cancellationToken = default)
    {
        return Storage.DeleteFileAsync(storageKey, cancellationToken);
    }
}
