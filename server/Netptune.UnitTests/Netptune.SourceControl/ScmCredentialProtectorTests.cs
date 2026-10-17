using System.Text;

using FluentAssertions;

using Microsoft.AspNetCore.DataProtection;

using Netptune.Core.Entities;
using Netptune.Core.SourceControl;
using Netptune.SourceControl.Credentials;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public class ScmCredentialProtectorTests
{
    private const string Secret = "glpat-abcdefghijklmnop1234";

    private readonly ScmCredentialProtector Protector = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Protect_ShouldRoundTripWithoutStoringThePlainText()
    {
        var protectedSecret = Protector.Protect(Secret);

        Encoding.UTF8.GetString(protectedSecret).Should().NotContain(Secret);
        Protector.Unprotect(protectedSecret).Should().Be(Secret);
    }

    [Fact]
    public void CreateHint_ShouldRevealOnlyTheLastFourCharacters()
    {
        Protector.CreateHint($"  {Secret}  ").Should().Be("1234");
    }

    [Fact]
    public void CreateHint_ShouldRevealNothingOfASecretTooShortToHint()
    {
        Protector.CreateHint("abcd1234").Should().BeEmpty();
    }

    [Fact]
    public void ToScmConnection_ShouldCarryTheStoredSecretsStillEncrypted()
    {
        var protectedCredential = Protector.Protect(Secret);

        var connection = new SourceControlConnection
        {
            Id = 7,
            WorkspaceId = 3,
            Provider = SourceControlProvider.GitLab,
            BaseUrl = "https://gitlab.example",
            ExternalAccountId = "group-1",
            AccountLogin = "acme",
            Status = ScmConnectionStatus.Active,
            GrantedCapabilities = SourceControlCapabilities.ListRepositories,
            CredentialKind = ScmCredentialKind.PersonalAccessToken,
            Credential = protectedCredential,
        };

        var scmConnection = connection.ToScmConnection();

        scmConnection.Id.Should().Be(7);
        scmConnection.WorkspaceId.Should().Be(3);
        scmConnection.Provider.Should().Be(SourceControlProvider.GitLab);
        scmConnection.BaseUrl.Should().Be("https://gitlab.example");
        scmConnection.ExternalAccountId.Should().Be("group-1");
        scmConnection.GrantedCapabilities.Should().Be(SourceControlCapabilities.ListRepositories);
        scmConnection.CredentialKind.Should().Be(ScmCredentialKind.PersonalAccessToken);
        scmConnection.ProtectedCredential.Should().BeSameAs(protectedCredential);
    }
}
