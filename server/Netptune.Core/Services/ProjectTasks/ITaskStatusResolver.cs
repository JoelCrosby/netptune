using Netptune.Core.Entities;

namespace Netptune.Core.Services.ProjectTasks;

// Picks the task status a write lands on. The two methods are the two policies callers need, and they
// differ in what a missing status means: a status the caller named explicitly is either found or an
// error, while the default chain keeps falling back until it finds something usable.
public interface ITaskStatusResolver
{
    Task<Status?> ResolveRequested(int statusId, int workspaceId, CancellationToken cancellationToken = default);

    Task<Status?> ResolveDefault(int? preferredStatusId, int workspaceId, CancellationToken cancellationToken = default);
}
