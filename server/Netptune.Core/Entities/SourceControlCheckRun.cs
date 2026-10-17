using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.Entities;

public sealed record SourceControlCheckRun : KeyedEntity<long>
{
    public int WorkspaceId { get; init; }

    public int RepoId { get; init; }

    public required string ExternalId { get; init; }

    public required string Name { get; set; }

    public required string CommitSha { get; init; }

    public ScmCheckRunStatus Status { get; set; }

    public ScmCheckRunConclusion? Conclusion { get; set; }

    public required string WebUrl { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public SourceControlRepo Repo { get; set; } = null!;

    #endregion
}
