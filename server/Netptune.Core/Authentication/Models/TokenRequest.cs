namespace Netptune.Core.Authentication.Models;

public class TokenRequest
{
    public required string Email { get; init; }

    public string? Password { get; init; }
}
