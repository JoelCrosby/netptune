using Netptune.Core.Meta;
using Netptune.Core.Storage;

namespace Netptune.Core.ViewModels.Workspace;

public class WorkspaceViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public WorkspaceMeta? MetaInfo { get; set; }

    public bool IsPublic { get; set; }

    public bool AssistantEnabled { get; set; } = true;

    public bool AllowAssistantDataSampling { get; set; } = true;

    public long MaxUploadBytes { get; set; } = UploadLimits.DefaultMaxUploadBytes;
}
