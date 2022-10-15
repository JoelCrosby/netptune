using Netptune.Core.Entities;

namespace Netptune.Core.Hubs;

public class UserConnection
{
    public string ConnectId { get; init; } = null!;

    public AppUser User { get; init; } = null!;

    public string UserId { get; init; } = null!;
}
