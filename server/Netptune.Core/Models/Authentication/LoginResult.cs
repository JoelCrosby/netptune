using Netptune.Core.Authentication.Models;

namespace Netptune.Core.Models.Authentication;

public sealed class LoginResult
{
    public bool IsSuccess { get; }

    public AuthenticationTicket? Ticket { get; }

    public string? Message { get; }

    private LoginResult(bool success, AuthenticationTicket? ticket, string? message = null)
    {
        IsSuccess = success;
        Ticket = ticket;
        Message = message;
    }

    public static LoginResult Success(AuthenticationTicket ticket)
    {
        return new(true, ticket);
    }

    public static LoginResult Failed(string? message = null)
    {
        return new(false, null, message);
    }
}
