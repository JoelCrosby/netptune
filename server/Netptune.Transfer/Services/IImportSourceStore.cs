using Netptune.Transfer.Entities;

namespace Netptune.Transfer.Services;

public interface IImportSourceStore
{
    Task<string> Save(string workspaceSlug, Guid publicId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> Open(ImportSession session, CancellationToken cancellationToken = default);

    Task Delete(string storageKey, CancellationToken cancellationToken = default);
}
