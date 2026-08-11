using Netptune.Core.Entities;
using Netptune.Core.Storage;

namespace Netptune.Core.Models.Options;

public sealed class WorkspaceStorageOptions
{
    public long DefaultWorkspaceLimitBytes { get; set; } = Workspace.DefaultStorageLimitBytes;

    public long DefaultMaxUploadBytes { get; set; } = UploadLimits.DefaultMaxUploadBytes;
}
