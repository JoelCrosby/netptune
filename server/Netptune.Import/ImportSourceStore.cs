using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Services;

namespace Netptune.Import;

public sealed class ImportSourceStore : IImportSourceStore
{
    // One JSON object per line with task, id, authorEmail, authorName, createdAt and body. Only sources
    // that fetch through an API write one. It lives in a sub-folder because the rows file keeps the
    // uploader's file name, and an upload called comments.ndjson would otherwise be read as comments.
    public const string CommentsFileName = "comments.ndjson";

    private const string SidecarFolder = "sidecar";

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
        var safeFileName = Path.GetFileName(fileName);
        var storageKey = $"{PathConstants.ImportPath(workspaceSlug)}{publicId:N}/{safeFileName}";
        var uploadOptions = new StorageUploadOptions
        {
            Name = safeFileName,
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

    public async Task<Stream?> OpenComments(ImportSession session, CancellationToken cancellationToken = default)
    {
        var source = await Storage.OpenReadAsync(CommentsKey(session.StorageKey), cancellationToken);

        return source;
    }

    public Task Delete(string storageKey, CancellationToken cancellationToken = default)
    {
        return Storage.DeleteFilesAsync([storageKey, CommentsKey(storageKey)], cancellationToken);
    }

    public static string CommentsKey(string storageKey)
    {
        var folder = storageKey[..(storageKey.LastIndexOf('/') + 1)];

        return $"{folder}{SidecarFolder}/{CommentsFileName}";
    }
}
