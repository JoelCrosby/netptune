using Netptune.Core.Meta;

namespace Netptune.Core.ViewModels.Workspace;

public class WorkspaceViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public WorkspaceMeta? MetaInfo { get; set; }
}
