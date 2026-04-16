namespace Netptune.Core.Authentication.Models;

public class AuthenticationTicket
{
    public required string UserId { get; init; }

    public required string EmailAddress { get; init; }

    public required string DisplayName { get; init; }

    public string? PictureUrl { get; init; }

    public required object Token { get; init; }

    public required string RefreshToken { get; init; }

    public DateTime Issued { get; init; }

    public DateTime Expires { get; init; }
}
