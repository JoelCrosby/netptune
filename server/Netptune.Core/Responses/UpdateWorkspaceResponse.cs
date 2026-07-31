using Netptune.Core.Entities;

namespace Netptune.Core.Responses;

public sealed class UpdateWorkspaceResponse
{
    public Workspace Workspace { get; init; } = null!;

    public string? PreviousSlug { get; init; }
}
