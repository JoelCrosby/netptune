namespace Netptune.Core.SourceControl.History;

public interface IScmHistorySync
{
    Task Sync(int repoId, CancellationToken cancellationToken);
}
