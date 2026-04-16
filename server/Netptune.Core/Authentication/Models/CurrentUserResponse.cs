using Netptune.Core.Models;

namespace Netptune.Core.Authentication.Models;

public class CurrentUserResponse
{
    public required string UserId { get; init; }

    public required string EmailAddress { get; init; }

    public required string DisplayName { get; init; }

    public string? PictureUrl { get; init; }

    public required UserPermissions UserPermissions { get; init; }
}
