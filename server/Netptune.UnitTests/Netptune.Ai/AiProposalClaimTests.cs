using FluentAssertions;

using Netptune.Ai.Execution;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiProposalClaimTests
{
    [Theory]
    [InlineData("I have proposed renaming the task.")]
    [InlineData("The change set is ready for you to apply.")]
    [InlineData("These are awaiting your approval.")]
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

    [Fact]
    public void IsUnbacked_ShouldIgnoreAClaimBackedByAProposal()
    {
        AiProposalClaim.IsUnbacked("I have proposed renaming the task.", 1).Should().BeFalse();
    }
}
