namespace Netptune.Core.Services.Ai;

public interface IAiCredentialProtector
{
    byte[] Protect(string secret);

    string Unprotect(byte[] protectedSecret);

    string CreateHint(string secret);
}
