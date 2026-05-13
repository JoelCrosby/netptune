namespace Netptune.Core.Authentication.Models;

public sealed class ExternalLoginLink
{
    public required string Token { get; init; }

    public required string Provider { get; init; }

    public required string Email { get; init; }
}
