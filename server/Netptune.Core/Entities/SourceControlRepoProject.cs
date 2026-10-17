using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public sealed record SourceControlRepoProject : KeyedEntity<int>
{
    public int RepoId { get; init; }

    public int ProjectId { get; init; }

    #region NavigationProperties

    [JsonIgnore]
    public SourceControlRepo Repo { get; set; } = null!;

    [JsonIgnore]
    public Project Project { get; set; } = null!;

    #endregion
}
