namespace Netptune.Core.Models.Authentication;

public class WorkspaceInvite
{
    public string Email { get; init; } = null!;

    public int WorkspaceId { get; init; }
}
