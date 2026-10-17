using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;

namespace Netptune.SourceControl.Connect;

internal sealed class ScmConnectStateProtector
{
    private const string PurposeName = "netptune.scm-connect-state";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    private readonly ITimeLimitedDataProtector Protector;

    public ScmConnectStateProtector(IDataProtectionProvider provider)
    {
        Protector = provider.CreateProtector(PurposeName).ToTimeLimitedDataProtector();
    }

    public string Protect(ScmConnectState state)
    {
        var json = JsonSerializer.Serialize(state);

        return Protector.Protect(json, Lifetime);
    }

    public ScmConnectState? Unprotect(string protectedState)
    {
        try
        {
            var json = Protector.Unprotect(protectedState, out _);

            return JsonSerializer.Deserialize<ScmConnectState>(json);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
