namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmConnectResult
{
    public required string ExternalAccountId { get; init; }

    public required ScmAccount Account { get; init; }

    public ScmCredentialKind CredentialKind { get; init; }

    public byte[]? ProtectedCredential { get; init; }

    public SourceControlCapabilities GrantedCapabilities { get; init; }
}
