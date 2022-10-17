namespace Netptune.Core.Authentication.Models;

public class TokenRequest
{
    public string Email { get; init; } = null!;

    public string? Password { get; init; }
}
