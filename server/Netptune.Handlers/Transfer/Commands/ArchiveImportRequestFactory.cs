using Netptune.Core.Services;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record ImportArchiveRequest
{
    public required Stream Archive { get; init; }

    public ArchiveImportMode Mode { get; init; }

    // Slug for the workspace created by ArchiveImportMode.Clone.
    public string? TargetSlug { get; init; }

    public bool InviteUnmatchedMembers { get; init; }
}

internal static class ArchiveImportRequestFactory
{
    public static async Task<ArchiveImportRequest> Build(ImportArchiveRequest request, IIdentityService identity)
    {
        return new ArchiveImportRequest
        {
            Archive = request.Archive,
            UserId = identity.GetCurrentUserId(),
            Mode = request.Mode,
            WorkspaceId = await identity.GetWorkspaceId(),
            TargetSlug = request.TargetSlug,
            InviteUnmatchedMembers = request.InviteUnmatchedMembers,
        };
    }
}
