using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public sealed class GitHubAppTokenMinterTests : IDisposable
{
    private readonly GitHubTestHarness Harness = new();

    [Fact]
    public async Task CreateAppJwt_ShouldBeSignedByTheAppPrivateKey()
    {
        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(GitHubTestHarness.PrivateKeyPem);

        var jwt = Harness.TokenMinter.CreateAppJwt();

        var validation = new TokenValidationParameters
        {
            ValidIssuer = GitHubTestHarness.AppId,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = new RsaSecurityKey(publicKey.ExportParameters(includePrivateParameters: false)),
        };

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(jwt, validation);

        result.IsValid.Should().BeTrue(result.Exception?.Message);
    }

    [Fact]
    public void CreateAppJwt_ShouldBackdateIssueAndExpireInsideGitHubsTenMinuteLimit()
    {
        var now = Harness.Clock.GetUtcNow();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(Harness.TokenMinter.CreateAppJwt());

        token.Header.Alg.Should().Be(SecurityAlgorithms.RsaSha256);
        token.Issuer.Should().Be(GitHubTestHarness.AppId);
        token.IssuedAt.Should().Be(now.AddSeconds(-60).UtcDateTime);
        token.ValidTo.Should().Be(now.AddMinutes(9).UtcDateTime);
    }

    [Fact]
    public void Constructor_ShouldAcceptAPrivateKeyWithEscapedNewlines()
    {
        var escaped = GitHubTestHarness.PrivateKeyPem.Replace("\n", "\\n");

        using var harness = new GitHubTestHarness(options => options.PrivateKey = escaped);

        harness.TokenMinter.CreateAppJwt().Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        Harness.Dispose();
    }
}
