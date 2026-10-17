namespace Netptune.Core.SourceControl;

public interface IScmCredentialProtector
{
    byte[] Protect(string secret);

    string Unprotect(byte[] protectedSecret);

    string CreateHint(string secret);
}
