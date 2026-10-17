using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.SourceControl;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.SourceControl;

public class ScmAuthorMatcherTests
{
    private static readonly AppUser ById = Member("by-id", "id@example.com");
    private static readonly AppUser ByName = Member("by-name", "name@example.com");
    private static readonly AppUser ByEmail = Member("by-email", "dev@example.com");

    private readonly ScmAuthorMatcher Matcher = new(
        [Identity(ById, externalId: "42", username: null), Identity(ByName, externalId: null, username: "Octo")],
        [ByEmail]);

    [Fact]
    public void Match_ShouldPreferTheAccountIdOverTheUsername()
    {
        Matcher.Match(SourceControlProvider.GitHub, "42", "octo", null).Should().BeSameAs(ById);
    }

    [Fact]
    public void Match_ShouldMatchAUsernameIgnoringCase()
    {
        Matcher.Match(SourceControlProvider.GitHub, null, "OCTO", null).Should().BeSameAs(ByName);
    }

    [Fact]
    public void Match_ShouldFallBackToTheCommitEmail()
    {
        Matcher.Match(SourceControlProvider.GitHub, "999", "stranger", " Dev@Example.com ").Should().BeSameAs(ByEmail);
    }

    [Fact]
    public void Match_ShouldNotMatchAnIdentityFromAnotherProvider()
    {
        Matcher.Match(SourceControlProvider.GitLab, "42", "octo", null).Should().BeNull();
    }

    private static AppUser Member(string id, string email)
    {
        return new AppUser { Id = id, Email = email, NormalizedEmail = email.ToUpperInvariant() };
    }

    private static UserSourceControlIdentity Identity(AppUser user, string? externalId, string? username)
    {
        var identity = new UserSourceControlIdentity
        {
            UserId = user.Id,
            Provider = SourceControlProvider.GitHub,
            ExternalId = externalId,
            User = user,
        };

        identity.SetUsername(username);

        return identity;
    }
}
