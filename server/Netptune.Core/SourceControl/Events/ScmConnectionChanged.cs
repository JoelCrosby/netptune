using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public sealed record ScmConnectionChanged : ScmEvent
{
    public ScmConnectionAction Action { get; init; }

    public required IReadOnlyList<ScmRepositoryRef> Repositories { get; init; }
}
