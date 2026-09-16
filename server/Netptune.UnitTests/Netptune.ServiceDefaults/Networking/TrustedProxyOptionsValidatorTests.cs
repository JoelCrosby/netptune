using FluentAssertions;

using Netptune.ServiceDefaults.Networking;

using Xunit;

namespace Netptune.UnitTests.Netptune.ServiceDefaults.Networking;

public class TrustedProxyOptionsValidatorTests
{
    private readonly TrustedProxyOptionsValidator Validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenNoEdgeCheckIsConfigured()
    {
        var result = Validator.Validate(null, new TrustedProxyOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenBothHalvesOfTheEdgeCheckAreConfigured()
    {
        var options = new TrustedProxyOptions
        {
            EdgeAuthorizationHeader = "X-Netptune-Edge",
            EdgeAuthorizationSecret = "secret",
        };

        var result = Validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTheSecretHasNoHeaderToArriveIn()
    {
        var options = new TrustedProxyOptions { EdgeAuthorizationSecret = "secret" };

        var result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTheHeaderIsNamedButNoSecretIsSet()
    {
        var options = new TrustedProxyOptions { EdgeAuthorizationHeader = "X-Netptune-Edge" };

        var result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenANetworkIsNotACidrRange()
    {
        var options = new TrustedProxyOptions { Networks = ["10.0.0.0/8", "not-a-network"] };

        var result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("not-a-network");
    }
}
