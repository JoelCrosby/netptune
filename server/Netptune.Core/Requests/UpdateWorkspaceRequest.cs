using System.ComponentModel.DataAnnotations;

using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public class UpdateWorkspaceRequest
{
    [Required]
    public string? Slug { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public WorkspaceMeta? MetaInfo { get; init; }
}
