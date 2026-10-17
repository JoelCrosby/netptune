using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;

namespace Netptune.Core.Entities;

public sealed record SourceControlConnection : WorkspaceEntity<int>
{
    public SourceControlProvider Provider { get; init; }

    public string? BaseUrl { get; init; }

    public required string ExternalAccountId { get; init; }

    public required string AccountLogin { get; set; }

    public ScmAccountType AccountType { get; set; }

    public string? AccountAvatarUrl { get; set; }

    public ScmConnectionStatus Status { get; set; }

    public SourceControlCapabilities GrantedCapabilities { get; set; }

    public ScmCredentialKind CredentialKind { get; set; }

    public byte[]? Credential { get; set; }

    public string? CredentialHint { get; set; }

    public DateTime? CredentialExpiresAt { get; set; }

    public byte[]? WebhookSecret { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<SourceControlRepo> Repos { get; set; } = new HashSet<SourceControlRepo>();

    #endregion

    public ScmConnection ToScmConnection()
    {
        return new ScmConnection
        {
            Id = Id,
            WorkspaceId = WorkspaceId,
            Provider = Provider,
            BaseUrl = BaseUrl,
            ExternalAccountId = ExternalAccountId,
            Status = Status,
            GrantedCapabilities = GrantedCapabilities,
            CredentialKind = CredentialKind,
            ProtectedCredential = Credential,
            ProtectedWebhookSecret = WebhookSecret,
        };
    }
}
