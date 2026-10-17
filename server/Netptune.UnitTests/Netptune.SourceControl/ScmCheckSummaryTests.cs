using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public class ScmCheckSummaryTests
{
    private long NextId;

    [Fact]
    public void Summarise_ShouldReturnNullWithoutAnyRuns()
    {
        ScmCheckSummary.Summarise([]).Should().BeNull();
    }

    [Fact]
    public void Summarise_ShouldFailWhenAnyRunFailed()
    {
        var summary = ScmCheckSummary.Summarise(
        [
            Run("build", ScmCheckRunConclusion.Success),
            Run("test", ScmCheckRunConclusion.TimedOut),
            Run("lint", status: ScmCheckRunStatus.InProgress),
        ]);

        summary!.State.Should().Be(ScmCheckState.Failure);
        summary.Failed.Should().Be(1);
        summary.Pending.Should().Be(1);
        summary.WebUrl.Should().Be("https://scm.test/runs/test");
    }

    [Fact]
    public void Summarise_ShouldBePendingWhileARunIsUnfinished()
    {
        var summary = ScmCheckSummary.Summarise(
        [
            Run("build", ScmCheckRunConclusion.Success),
            Run("lint", status: ScmCheckRunStatus.Queued),
        ]);

        summary!.State.Should().Be(ScmCheckState.Pending);
    }

    [Fact]
    public void Summarise_ShouldOnlyCountTheLatestRunOfEachName()
    {
        var summary = ScmCheckSummary.Summarise(
        [
            Run("build", ScmCheckRunConclusion.Failure),
            Run("build", ScmCheckRunConclusion.Success),
        ]);

        summary!.State.Should().Be(ScmCheckState.Success);
        summary.Total.Should().Be(1);
        summary.Passed.Should().Be(1);
    }

    [Fact]
    public void Summarise_ShouldBeNeutralWhenNothingPassedOrFailed()
    {
        var summary = ScmCheckSummary.Summarise(
        [
            Run("build", ScmCheckRunConclusion.Skipped),
            Run("deploy", ScmCheckRunConclusion.Cancelled),
        ]);

        summary!.State.Should().Be(ScmCheckState.Neutral);
        summary.WebUrl.Should().BeNull();
    }

    private SourceControlCheckRun Run(
        string name,
        ScmCheckRunConclusion? conclusion = null,
        ScmCheckRunStatus status = ScmCheckRunStatus.Completed)
    {
        return new SourceControlCheckRun
        {
            Id = ++NextId,
            ExternalId = NextId.ToString(),
            Name = name,
            CommitSha = "abc",
            Status = status,
            Conclusion = conclusion,
            WebUrl = $"https://scm.test/runs/{name}",
        };
    }
}
