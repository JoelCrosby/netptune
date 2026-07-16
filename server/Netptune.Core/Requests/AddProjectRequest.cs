using System.ComponentModel.DataAnnotations;

using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public record AddProjectRequest
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? RepositoryUrl { get; set; }

    [Required]
    public ProjectMeta MetaInfo { get; set; } = null!;

    public int? DefaultStatusId { get; set; }

    public string? TemplateKey { get; init; }
}
