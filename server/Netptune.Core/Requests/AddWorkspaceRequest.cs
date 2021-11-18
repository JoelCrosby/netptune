using System.ComponentModel.DataAnnotations;

using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public class AddWorkspaceRequest
{
    [Required]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required]
    public string Slug { get; set; }

    [Required]
    public WorkspaceMeta MetaInfo { get; set; }
}