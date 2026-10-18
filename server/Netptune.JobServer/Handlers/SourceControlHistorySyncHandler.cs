using Mediator;

using Netptune.Core.SourceControl.History;

namespace Netptune.JobServer.Handlers;

public sealed class SourceControlHistorySyncHandler(IScmHistorySync historySync)
    : IRequestHandler<ScmHistorySyncMessage>
{
    public async ValueTask<Unit> Handle(ScmHistorySyncMessage request, CancellationToken cancellationToken)
    {
        await historySync.Sync(request.RepoId, cancellationToken);

        return Unit.Value;
    }
}
