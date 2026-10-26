namespace Netptune.Core.Models.Workspaces;

public sealed record DeletedWorkspaceReferences
{
    public List<string> MemberUserIds { get; init; } = [];

    public List<int> TaskIds { get; init; } = [];

    public List<int> ProjectIds { get; init; } = [];

    public List<int> BoardIds { get; init; } = [];

    public List<int> SprintIds { get; init; } = [];

    public List<int> PageIds { get; init; } = [];

    public List<string> StorageKeys { get; init; } = [];
}
