namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmConnectOutcome
{
    public ScmConnectResult? Connection { get; init; }

    public ScmConnectChoice? Choice { get; init; }

    public static ScmConnectOutcome Connected(ScmConnectResult connection)
    {
        return new ScmConnectOutcome { Connection = connection };
    }

    public static ScmConnectOutcome Choose(ScmConnectChoice choice)
    {
        return new ScmConnectOutcome { Choice = choice };
    }
}
