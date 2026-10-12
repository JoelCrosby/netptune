using System.Security.Cryptography;

using FluentAssertions;

using Netptune.Core.Authentication;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Authentication;

public class ApiCredentialTokenTests
{
    [Fact]
    public void Create_ShouldProduceAParsableTokenWhoseSecretMatchesTheStoredHash()
    {
        var credentialId = Guid.NewGuid();

        var generated = ApiCredentialToken.Create(credentialId);
        var parsed = ApiCredentialToken.TryParse(generated.Token, out var parsedId, out var secretHash);

        parsed.Should().BeTrue();
        parsedId.Should().Be(credentialId);
        generated.Prefix.Should().Be($"ntp_{credentialId:N}"[..20]);
        CryptographicOperations.FixedTimeEquals(secretHash, generated.SecretHash).Should().BeTrue();
    }

    [Fact]
    public void TryParse_ShouldAcceptASecretThatStartsWithAnUnderscore()
    {
        var credentialId = Guid.NewGuid();
        var secret = new byte[32];
        secret[0] = 0xFF;
        var encodedSecret = Convert.ToBase64String(secret).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var token = $"ntp_{credentialId:N}_{encodedSecret}";

        var parsed = ApiCredentialToken.TryParse(token, out var parsedId, out var secretHash);

        encodedSecret.Should().StartWith("_");
        parsed.Should().BeTrue();
        parsedId.Should().Be(credentialId);
        secretHash.Should().Equal(SHA256.HashData(secret));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("ntp_invalid_secret")]
    [InlineData("ntp_00000000000000000000000000000000_AQ")]
    [InlineData("ntp_00000000000000000000000000000000_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void TryParse_ShouldRejectMalformedTokens(string token)
    {
        ApiCredentialToken.TryParse(token, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldRejectNonCanonicalBase64UrlSecret()
    {
        var credentialId = Guid.NewGuid();
        var nonCanonicalSecret = $"{new string('A', 42)}B";
        var token = $"ntp_{credentialId:N}_{nonCanonicalSecret}";

        ApiCredentialToken.TryParse(token, out _, out _).Should().BeFalse();
    }
}
