using System.ComponentModel.DataAnnotations;

using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public record UpdateWorkspaceRequest
{
    [Required]
    public string? Slug { get; init; }

    public string? NewSlug { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public WorkspaceMeta? MetaInfo { get; init; }

    public bool? IsPublic { get; init; }

    public bool? AssistantEnabled { get; init; }

    public bool? AllowAssistantDataSampling { get; init; }

    public List<string>? PublicPermissions { get; init; }

    public long? MaxUploadBytes { get; init; }
}
