using AspNet.Security.OAuth.GitHub;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace Netptune.Services.Authentication;

public static class AuthenticationSchemes
{
    public const string Github = GitHubAuthenticationDefaults.AuthenticationScheme;
    public const string Google = GoogleDefaults.AuthenticationScheme;
    public const string Microsoft = MicrosoftAccountDefaults.AuthenticationScheme;
}
