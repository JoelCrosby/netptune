namespace Netptune.Core.Authentication.Models;

public class RegisterRequest : TokenRequest
{
    public string Firstname { get; init; } = null!;

    public string Lastname { get; init; } = null!;

    public string? InviteCode { get; init; }

    public string? PictureUrl { get; init; }

    public AuthenticationProvider AuthenticationProvider { get; init; }
}
