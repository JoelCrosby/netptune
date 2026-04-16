namespace Netptune.Core.Authentication.Models;

public class RegisterRequest : TokenRequest
{
    public required string Firstname { get; init; }

    public required string Lastname { get; init; }

    public string? InviteCode { get; init; }

    public string? PictureUrl { get; init; }

    public AuthenticationProvider AuthenticationProvider { get; init; }
}
