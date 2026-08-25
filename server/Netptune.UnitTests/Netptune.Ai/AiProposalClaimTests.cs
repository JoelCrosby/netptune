using FluentAssertions;

using Netptune.Ai.Execution;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiProposalClaimTests
{
    [Theory]
    [InlineData("I have proposed renaming the task.")]
    [InlineData("I've proposed a sprint for next week.")]
    [InlineData("We proposed moving both tasks to the backlog.")]
    [InlineData("The change set is ready for you to apply.")]
    [InlineData("The change set now contains three changes.")]
    [InlineData("These are awaiting your approval.")]
    [InlineData("The rename is pending your approval.")]
    [InlineData("The change set is empty.")]
    public void IsUnbacked_ShouldFlagAClaimWithNothingBehindIt(string text)
    {
        AiProposalClaim.IsUnbacked(text, 0).Should().BeTrue();
    }

    [Theory]
    [InlineData("There are four tasks left in the sprint.")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsUnbacked_ShouldIgnoreAReplyThatClaimsNothing(string text)
    {
        AiProposalClaim.IsUnbacked(text, 0).Should().BeFalse();
    }

    [Theory]
    [InlineData("I can propose new tasks, retag existing ones and plan a sprint for you.")]
    [InlineData("Would you like me to propose adding it to the current sprint?")]
    [InlineData("Anything I write goes into a change set that you review and apply yourself.")]
    [InlineData("Tell me which project you mean and I'll propose the change.")]
    public void IsUnbacked_ShouldIgnoreAReplyThatOnlyDescribesWhatItCanDo(string text)
    {
        AiProposalClaim.IsUnbacked(text, 0).Should().BeFalse();
    }

    [Fact]
    public void IsUnbacked_ShouldIgnoreAClaimBackedByAProposal()
    {
        AiProposalClaim.IsUnbacked("I have proposed renaming the task.", 1).Should().BeFalse();
    }
}
