using Flurl;

namespace Netptune.Handlers.Users;

internal static class WorkspaceInviteLinks
{
    public static string Create(string clientOrigin, string code, bool hasAccount)
    {
        if (hasAccount)
        {
            return clientOrigin
                .AppendPathSegments("auth", "accept-invite")
                .SetQueryParam("code", code, true)
                .ToString();
        }

        return clientOrigin
            .AppendPathSegments("auth", "register")
            .SetQueryParam("code", code, true)
            .SetQueryParam("refer", "invite")
            .ToString();
    }
}
