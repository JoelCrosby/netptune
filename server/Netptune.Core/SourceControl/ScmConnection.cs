namespace Netptune.Core.SourceControl;

public sealed record ScmConnection
{
    public int Id { get; init; }

    public int WorkspaceId { get; init; }

    public SourceControlProvider Provider { get; init; }

    public string? BaseUrl { get; init; }

    public required string ExternalAccountId { get; init; }

    public ScmConnectionStatus Status { get; init; }

    public SourceControlCapabilities GrantedCapabilities { get; init; }

    public ScmCredentialKind CredentialKind { get; init; }

    public byte[]? ProtectedCredential { get; init; }

    public byte[]? ProtectedWebhookSecret { get; init; }
}
