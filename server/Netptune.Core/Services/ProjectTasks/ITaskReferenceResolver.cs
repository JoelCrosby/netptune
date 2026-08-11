using Netptune.Core.Entities;

namespace Netptune.Core.Services.ProjectTasks;

public sealed record TaskAssigneeResolution
{
    public bool ShouldUpdate { get; init; }

    public IReadOnlyList<string> UserIds { get; init; } = [];

    public string Error { get; init; } = string.Empty;

    public bool IsValid => Error.Length == 0;

    public static TaskAssigneeResolution Failed(string error) => new() { Error = error };

    public static TaskAssigneeResolution Unchanged() => new();
}

public sealed record TaskTagResolution
{
    public bool ShouldUpdate { get; init; }

    public IReadOnlyList<Tag> Tags { get; init; } = [];

    public string Error { get; init; } = string.Empty;

    public bool IsValid => Error.Length == 0;

    public static TaskTagResolution Failed(string error) => new() { Error = error };

    public static TaskTagResolution Unchanged() => new();
}

// Turns the user ids and tag names carried by a task request into workspace members and workspace
// tags, refusing anything that does not belong to the workspace. A null collection means the caller
// did not ask for a change, which the resolutions report as ShouldUpdate false; an empty one asks for
// everything to be cleared. Callers decide how to apply what comes back.
public interface ITaskReferenceResolver
{
    Task<TaskAssigneeResolution> ResolveAssignees(IReadOnlyCollection<string>? userIds, int workspaceId, CancellationToken cancellationToken = default);

    Task<TaskTagResolution> ResolveTags(IReadOnlyCollection<string>? tagNames, int workspaceId, CancellationToken cancellationToken = default);
}
