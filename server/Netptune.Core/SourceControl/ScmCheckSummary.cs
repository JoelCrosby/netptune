using Netptune.Core.Entities;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Core.SourceControl;

public static class ScmCheckSummary
{
    // A re-run arrives as a new run with the same name, so only the latest run of each name counts.
    public static ScmCheckSummaryViewModel? Summarise(IEnumerable<SourceControlCheckRun> checkRuns)
    {
        var latest = checkRuns
            .GroupBy(checkRun => checkRun.Name, StringComparer.Ordinal)
            .Select(group => group.MaxBy(checkRun => checkRun.Id)!)
            .OrderBy(checkRun => checkRun.Name, StringComparer.Ordinal)
            .ToList();

        if (latest.Count == 0)
        {
            return null;
        }

        var failed = latest.Where(IsFailure).ToList();
        var pending = latest.Count(checkRun => checkRun.Status != ScmCheckRunStatus.Completed);
        var passed = latest.Count(checkRun => checkRun.Conclusion == ScmCheckRunConclusion.Success);

        var firstFailure = failed.FirstOrDefault();
        var onlyRun = latest.Count == 1 ? latest[0] : null;
        var attentionRun = firstFailure ?? onlyRun;

        return new ScmCheckSummaryViewModel
        {
            State = StateOf(failed.Count, pending, passed),
            Total = latest.Count,
            Passed = passed,
            Failed = failed.Count,
            Pending = pending,
            WebUrl = attentionRun?.WebUrl,
        };
    }

    private static ScmCheckState StateOf(int failed, int pending, int passed)
    {
        if (failed > 0)
        {
            return ScmCheckState.Failure;
        }

        if (pending > 0)
        {
            return ScmCheckState.Pending;
        }

        return passed > 0 ? ScmCheckState.Success : ScmCheckState.Neutral;
    }

    private static bool IsFailure(SourceControlCheckRun checkRun)
    {
        return checkRun.Conclusion is ScmCheckRunConclusion.Failure or ScmCheckRunConclusion.TimedOut;
    }
}
