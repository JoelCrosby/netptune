namespace Netptune.Core.Authentication.Models;

public sealed record AcceptWorkspaceInviteResponse
{
    public required string WorkspaceSlug { get; init; }
}
