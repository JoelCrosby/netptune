namespace Netptune.Core.SourceControl.Checks;

public interface IScmConnectionChecker
{
    ValueTask<IReadOnlyList<ScmConnectionCheck>> Check(ScmConnection connection, CancellationToken cancellationToken);

    ScmConnectionCheckDescription Describe(ScmConnectionCheck check);
}
