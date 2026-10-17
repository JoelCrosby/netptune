using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.Entities;

public sealed record TaskCodeLink : KeyedEntity<long>
{
    public int WorkspaceId { get; init; }

    public int TaskId { get; init; }

    public int RepoId { get; init; }

    public TaskCodeLinkKind Kind { get; init; }

    // Branch name, commit SHA or pull request number, depending on Kind.
    public required string ExternalRef { get; init; }

    public required string Title { get; set; }

    public required string WebUrl { get; set; }

    public string? AuthorLogin { get; set; }

    public string? AuthorExternalId { get; set; }

    public string? AuthorEmail { get; set; }

    public ScmPullRequestState? PullRequestState { get; set; }

    public string? HeadSha { get; set; }

    public ScmCheckState? LastCheckState { get; set; }

    public bool IsRemoved { get; set; }

    public DateTime OccurredAt { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public SourceControlRepo Repo { get; set; } = null!;

    [JsonIgnore]
    public ProjectTask Task { get; set; } = null!;

    #endregion
}
