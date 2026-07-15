using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public sealed class WorkspaceFileFilter : PageRequest
{
    public string? Query { get; init; }

    public WorkspaceFilePurpose? Purpose { get; init; }

    public string? ContentTypeGroup { get; init; }

    public string? UploadedByUserId { get; init; }

    public string? TaskSystemId { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }
}
