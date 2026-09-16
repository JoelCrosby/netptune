using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http;

namespace Netptune.ServiceDefaults.Networking;

public static class EdgeAuthorization
{
    public static bool IsSatisfied(HttpContext context, TrustedProxyOptions options)
    {
        if (!options.RequiresEdgeAuthorization)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.EdgeAuthorizationHeader))
        {
            return false;
        }

        var presented = context.Request.Headers[options.EdgeAuthorizationHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(presented))
        {
            return false;
        }

        return Matches(presented, options.EdgeAuthorizationSecret!);
    }

    private static bool Matches(string presented, string expected)
    {
        var presentedBytes = Encoding.UTF8.GetBytes(presented);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return CryptographicOperations.FixedTimeEquals(presentedBytes, expectedBytes);
    }
}
