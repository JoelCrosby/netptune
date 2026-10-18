using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.Entities;

public sealed record SourceControlRepo : KeyedEntity<int>
{
    public int WorkspaceId { get; init; }

    public int ConnectionId { get; init; }

    public required string ExternalId { get; init; }

    public required string Owner { get; set; }

    public required string Name { get; set; }

    public required string FullName { get; set; }

    public required string DefaultBranch { get; set; }

    public required string WebUrl { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsArchived { get; set; }

    public bool IsAccessible { get; set; }

    public bool IsLinked { get; set; }

    public DateTime? LinkedAt { get; set; }

    public string? LinkedByUserId { get; set; }

    public DateTime SyncedAt { get; set; }

    public DateTime? HistorySyncRequestedAt { get; set; }

    public DateTime? HistorySyncStartedAt { get; set; }

    public DateTime? HistorySyncedAt { get; set; }

    public string? HistorySyncError { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public SourceControlConnection Connection { get; set; } = null!;

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;

    [JsonIgnore]
    public ICollection<SourceControlRepoProject> Projects { get; set; } = new HashSet<SourceControlRepoProject>();

    #endregion

    public void RequestHistorySync(DateTime now)
    {
        HistorySyncRequestedAt = now;
        HistorySyncError = null;
    }

    public ScmRepositoryRef ToRepositoryRef()
    {
        return new ScmRepositoryRef(ExternalId, Owner, Name);
    }
}
