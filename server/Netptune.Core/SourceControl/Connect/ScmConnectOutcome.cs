using Netptune.Core.SourceControl.Checks;

namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmConnectOutcome
{
    public ScmConnectResult? Connection { get; init; }

    public ScmConnectChoice? Choice { get; init; }

    public IReadOnlyList<ScmConnectionCheck>? Checks { get; init; }

    public static ScmConnectOutcome Connected(ScmConnectResult connection)
    {
        return new ScmConnectOutcome { Connection = connection };
    }

    public static ScmConnectOutcome Choose(ScmConnectChoice choice)
    {
        return new ScmConnectOutcome { Choice = choice };
    }

    public static ScmConnectOutcome Checked(IReadOnlyList<ScmConnectionCheck> checks, ScmConnectResult? connection)
    {
        return new ScmConnectOutcome { Checks = checks, Connection = connection };
    }
}
