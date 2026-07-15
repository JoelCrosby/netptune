using Netptune.Core.Entities;

namespace Netptune.Core.Models.Options;

public sealed class WorkspaceStorageOptions
{
    public long DefaultWorkspaceLimitBytes { get; set; } = Workspace.DefaultStorageLimitBytes;
}
