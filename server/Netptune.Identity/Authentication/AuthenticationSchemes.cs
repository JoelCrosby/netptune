using AspNet.Security.OAuth.GitHub;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace Netptune.Identity.Authentication;

public static class AuthenticationSchemes
{
    public const string Smart = "Netptune";
    public const string ApiKey = "ApiKey";
    public const string Github = GitHubAuthenticationDefaults.AuthenticationScheme;
    public const string Google = GoogleDefaults.AuthenticationScheme;
    public const string Microsoft = MicrosoftAccountDefaults.AuthenticationScheme;
}
