namespace Netptune.Core.Authentication.Models;

public sealed record AcceptWorkspaceInviteRequest
{
    public required string Code { get; init; }
}
