using FluentAssertions;

using Netptune.Events;

using Xunit;

namespace Netptune.UnitTests.Netptune.Events;

public class EventRetryPolicyTests
{
    [Fact]
    public void Backoff_ShouldOpenWithAckWait_AndStayShorterThanTheDeliveryBudget()
    {
        var policy = EventRetryPolicy.Default;

        policy.Backoff[0].Should().Be(policy.AckWait);

        policy.Backoff.Should().HaveCountLessThan(policy.MaxDeliver);

        policy.Backoff.Should().BeInAscendingOrder();
    }
}
