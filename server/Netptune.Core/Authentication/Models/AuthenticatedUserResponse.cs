namespace Netptune.Core.Authentication.Models;

public record AuthenticatedUserResponse
{
    public required string UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public string? PictureUrl { get; init; }

    public DateTime Expires { get; init; }
}

public static class AuthenticationTicketExtensions
{
    public static AuthenticatedUserResponse ToUserResponse(this AuthenticationTicket ticket) =>
        new()
        {
            UserId = ticket.UserId,
            Email = ticket.EmailAddress,
            DisplayName = ticket.DisplayName,
            PictureUrl = ticket.PictureUrl,
            Expires = ticket.Expires,
        };
}
