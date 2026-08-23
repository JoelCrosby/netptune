using Netptune.Core.Services;
using Netptune.Transfer.Services;

namespace Netptune.Transfer.Archive;

// The archive upload as it arrives from the endpoint, before the caller's identity has been resolved
// into the user and workspace the import runs against.
public sealed record ImportArchiveRequest
{
    public required Stream Archive { get; init; }

    public ArchiveImportMode Mode { get; init; }

    public string? TargetSlug { get; init; }

    public bool InviteUnmatchedMembers { get; init; }

    public async Task<ArchiveImportRequest> Resolve(IIdentityService identity)
    {
        return new ArchiveImportRequest
        {
            Archive = Archive,
            UserId = identity.GetCurrentUserId(),
            Mode = Mode,
            WorkspaceId = await identity.GetWorkspaceId(),
            TargetSlug = TargetSlug,
            InviteUnmatchedMembers = InviteUnmatchedMembers,
        };
    }
}
