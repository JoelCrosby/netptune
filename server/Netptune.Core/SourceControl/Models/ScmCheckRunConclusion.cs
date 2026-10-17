namespace Netptune.Core.SourceControl.Models;

public enum ScmCheckRunConclusion
{
    Success = 0,
    Failure = 1,
    Cancelled = 2,
    Skipped = 3,
    Neutral = 4,
    TimedOut = 5,
}
