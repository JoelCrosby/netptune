using System.Text;

using Microsoft.AspNetCore.DataProtection;

using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Credentials;

internal sealed class ScmCredentialProtector : IScmCredentialProtector
{
    private const string PurposeName = "netptune.scm-credentials";
    private const int HintLength = 4;

    private readonly IDataProtector Protector;

    public ScmCredentialProtector(IDataProtectionProvider provider)
    {
        Protector = provider.CreateProtector(PurposeName);
    }

    public byte[] Protect(string secret)
    {
        var plainBytes = Encoding.UTF8.GetBytes(secret);

        return Protector.Protect(plainBytes);
    }

    public string Unprotect(byte[] protectedSecret)
    {
        var plainBytes = Protector.Unprotect(protectedSecret);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string CreateHint(string secret)
    {
        var trimmed = secret.Trim();

        if (trimmed.Length <= HintLength)
        {
            return trimmed;
        }

        return trimmed[^HintLength..];
    }
}
