using System.ComponentModel.DataAnnotations;

using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public class AddProjectRequest
{
    [Required]
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string RepositoryUrl { get; set; } = null!;

    public string Key { get; set; } = null!;

    [Required]
    public ProjectMeta MetaInfo { get; set; } = null!;
}
