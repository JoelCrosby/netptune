using FluentAssertions;

using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Webhooks;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Webhooks;

public class ScmPullRequestTransitionsTests
{
    [Theory]
    [InlineData(ScmPullRequestState.Open, ScmPullRequestState.Merged, ScmPullRequestAction.Merged)]
    [InlineData(ScmPullRequestState.Draft, ScmPullRequestState.Merged, ScmPullRequestAction.Merged)]
    [InlineData(ScmPullRequestState.Open, ScmPullRequestState.Closed, ScmPullRequestAction.Closed)]
    [InlineData(ScmPullRequestState.Closed, ScmPullRequestState.Open, ScmPullRequestAction.Reopened)]
    [InlineData(ScmPullRequestState.Closed, ScmPullRequestState.Draft, ScmPullRequestAction.Reopened)]
    [InlineData(ScmPullRequestState.Draft, ScmPullRequestState.Open, ScmPullRequestAction.ReadyForReview)]
    [InlineData(ScmPullRequestState.Open, ScmPullRequestState.Draft, ScmPullRequestAction.ConvertedToDraft)]
    [InlineData(ScmPullRequestState.Open, ScmPullRequestState.Open, ScmPullRequestAction.Edited)]
    [InlineData(ScmPullRequestState.Merged, ScmPullRequestState.Merged, ScmPullRequestAction.Edited)]
    [InlineData(ScmPullRequestState.Merged, ScmPullRequestState.Closed, ScmPullRequestAction.Edited)]
    public void Derive_ShouldNameTheChangeBetweenTheStoredAndCurrentState(
        ScmPullRequestState previous,
        ScmPullRequestState current,
        ScmPullRequestAction expected)
    {
        var action = ScmPullRequestTransitions.Derive(previous, current);

        action.Should().Be(expected);
    }

    [Theory]
    [InlineData(ScmPullRequestState.Open, ScmPullRequestAction.Edited)]
    [InlineData(ScmPullRequestState.Draft, ScmPullRequestAction.Edited)]
    [InlineData(ScmPullRequestState.Merged, ScmPullRequestAction.Merged)]
    [InlineData(ScmPullRequestState.Closed, ScmPullRequestAction.Closed)]
    public void Derive_ShouldOnlyReportAFinishedPullRequest_WhenNothingIsStored(
        ScmPullRequestState current,
        ScmPullRequestAction expected)
    {
        var action = ScmPullRequestTransitions.Derive(null, current);

        action.Should().Be(expected);
    }
}
