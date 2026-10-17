using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Models.Activity;

public class ActivityNotificationPolicyTests
{
    [Theory]
    [InlineData(ActivityType.PullRequestMerged)]
    [InlineData(ActivityType.CommitsPushed)]
    [InlineData(ActivityType.ChecksFailed)]
    [InlineData(ActivityType.LoginFailed)]
    public void IsNotifiable_ShouldBeFalseForActivityThatOnlyAppearsInHistory(ActivityType activityType)
    {
        ActivityNotificationPolicy.IsNotifiable(activityType).Should().BeFalse();
    }

    [Fact]
    public void IsNotifiable_ShouldBeTrueForTaskChanges()
    {
        ActivityNotificationPolicy.IsNotifiable(ActivityType.ModifyStatus).Should().BeTrue();
    }
}
