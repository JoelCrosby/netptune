namespace Netptune.Core.Authentication.Models;

public class CurrentUserResponse
{
    public string UserId { get; set; }

    public string EmailAddress { get; set; }

    public string DisplayName { get; set; }

    public string PictureUrl { get; set; }
}