using Netptune.Core.Authentication.Models;

namespace Netptune.Core.Models.Authentication;

public sealed class LoginResult
{
    public bool IsSuccess { get; }

    public AuthenticationTicket? Ticket { get; }

    public string? Message { get; }

    public ExternalLoginLink? ExternalLoginLink { get; }

    public bool IsLinkRequired => ExternalLoginLink is not null;

    private LoginResult(
        bool success,
        AuthenticationTicket? ticket,
        string? message = null,
        ExternalLoginLink? externalLoginLink = null)
    {
        IsSuccess = success;
        Ticket = ticket;
        Message = message;
        ExternalLoginLink = externalLoginLink;
    }

    public static LoginResult Success(AuthenticationTicket ticket)
    {
        return new(true, ticket);
    }

    public static LoginResult Failed(string? message = null)
    {
        return new(false, null, message);
    }

    public static LoginResult LinkRequired(ExternalLoginLink externalLoginLink)
    {
        return new(false, null, externalLoginLink: externalLoginLink);
    }
}
