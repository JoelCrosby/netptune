namespace Netptune.Core.Authentication.Models;

public class RegisterRequest : TokenRequest
{
    public string Firstname { get; set; }

    public string Lastname { get; set; }

    public string InviteCode { get; set; }

    public string PictureUrl { get; set; }

    public AuthenticationProvider AuthenticationProvider { get; set; }
}