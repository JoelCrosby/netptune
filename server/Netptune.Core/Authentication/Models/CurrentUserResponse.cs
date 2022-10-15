namespace Netptune.Core.Authentication.Models;

public class CurrentUserResponse
{
    public string UserId { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? PictureUrl { get; set; }
}
