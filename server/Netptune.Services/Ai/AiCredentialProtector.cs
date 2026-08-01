using System.Text;

using Microsoft.AspNetCore.DataProtection;

using Netptune.Core.Services.Ai;

namespace Netptune.Services.Ai;

public sealed class AiCredentialProtector : IAiCredentialProtector
{
    private const string PurposeName = "netptune.ai-credentials";
    private const int HintLength = 4;

    private readonly IDataProtector Protector;

    public AiCredentialProtector(IDataProtectionProvider provider)
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
