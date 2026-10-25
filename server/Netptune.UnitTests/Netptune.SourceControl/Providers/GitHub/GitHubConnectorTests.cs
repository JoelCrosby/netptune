using System.Net;

using FluentAssertions;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.SourceControl.Connect;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.GitHub;

public sealed class GitHubConnectorTests : IDisposable
{
    private const int WorkspaceId = 3;
    private const string UserId = "user-1";

    private const string AcmeMembershipUrl = "/user/memberships/orgs/acme";

    private const string AdminMembership = """{ "state": "active", "role": "admin" }""";

    private const string MemberMembership = """{ "state": "active", "role": "member" }""";

    private const string UserAccountInstallation = """
        { "id": 987654, "account": { "id": 42, "login": "jo", "type": "User" }, "permissions": { "metadata": "read" } }
        """;

    private const string Installation = """
        {
          "id": 987654,
          "account": { "id": 11, "login": "acme", "type": "Organization", "avatar_url": "https://avatars.example/acme" },
          "permissions": { "metadata": "read", "contents": "read", "pull_requests": "read", "checks": "read" },
          "suspended_at": null
        }
        """;

    private const string OtherOrganisationInstallation = """
        { "id": 111, "account": { "id": 12, "login": "globex", "type": "Organization" }, "permissions": { "metadata": "read" } }
        """;

    private const string SuspendedInstallation = """
        {
          "id": 222,
          "account": { "id": 13, "login": "initech", "type": "Organization" },
          "permissions": { "metadata": "read" },
          "suspended_at": "2026-10-01T00:00:00Z"
        }
        """;

    private const string AdministeredMemberships = """
        [
          { "state": "active", "role": "admin", "organization": { "id": 11, "login": "acme" } },
          { "state": "active", "role": "member", "organization": { "id": 12, "login": "globex" } },
          { "state": "active", "role": "admin", "organization": { "id": 13, "login": "initech" } }
        ]
        """;

    private const string SecondInstallationsPage = "https://api.github.com/user/installations?per_page=100&page=2";

    private readonly GitHubTestHarness Harness = new();

    [Fact]
    public async Task GetAccessToken_ShouldMintAnInstallationTokenOnceAndReuseIt()
    {
        Harness.StubInstallationToken();
        var connection = GitHubTestHarness.CreateConnection();

        var first = await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);
        var second = await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);

        first.Token.Should().Be("ghs_installation");
        second.Should().Be(first);
        Harness.Http.Requests.Should().ContainSingle();
        Harness.Http.Requests[0].Authorization.Should().StartWith("Bearer ey");
    }

    [Fact]
    public async Task GetAccessToken_ShouldMintAFreshTokenWhenTheCachedOneIsAboutToExpire()
    {
        Harness.StubInstallationToken();
        var connection = GitHubTestHarness.CreateConnection();

        await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);
        Harness.Clock.Advance(TimeSpan.FromMinutes(55));
        Harness.StubInstallationToken("ghs_refreshed");
        var refreshed = await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);

        refreshed.Token.Should().Be("ghs_refreshed");
        Harness.Http.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccessToken_ShouldThrowWhenGitHubDoesNotKnowTheInstallation()
    {
        Harness.Http.On(HttpMethod.Post, $"/app/installations/{GitHubTestHarness.InstallationId}/access_tokens", HttpStatusCode.NotFound, """{ "message": "Not Found" }""");

        var connection = GitHubTestHarness.CreateConnection();

        var getToken = async () => await Harness.Connector.GetAccessToken(
            connection,
            TestContext.Current.CancellationToken);

        await getToken.Should().ThrowAsync<ScmProviderException>();
    }

    [Fact]
    public async Task GetAccessToken_ShouldReportANetworkFailureAsAProviderError()
    {
        Harness.Http.On(
            HttpMethod.Post,
            $"/app/installations/{GitHubTestHarness.InstallationId}/access_tokens",
            HttpStatusCode.OK,
            configure: _ => throw new HttpRequestException("Connection refused"));

        var connection = GitHubTestHarness.CreateConnection();

        var getToken = async () => await Harness.Connector.GetAccessToken(
            connection,
            TestContext.Current.CancellationToken);

        var thrown = await getToken.Should().ThrowAsync<ScmProviderException>();

        thrown.Which.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetGrantedCapabilities_ShouldReadTheInstallationPermissions()
    {
        Harness.Http.On(
            HttpMethod.Get,
            $"/app/installations/{GitHubTestHarness.InstallationId}", HttpStatusCode.OK, Installation);

        var granted = await Harness.Connector.GetGrantedCapabilities(
            GitHubTestHarness.CreateConnection(),
            TestContext.Current.CancellationToken);

        granted.HasFlag(SourceControlCapabilities.ReadPullRequests).Should().BeTrue();
        Harness.Http.Requests.Single().Authorization.Should().StartWith("Bearer ey");
    }

    [Fact]
    public void BeginConnect_ShouldSendTheBrowserToUserAuthorisationWithSignedState()
    {
        var start = Harness.Connector.BeginConnect(new ScmConnectRequest(WorkspaceId, UserId));

        var redirect = new Uri(start.RedirectUrl);
        var query = ParseQuery(redirect);

        redirect.GetLeftPart(UriPartial.Path).Should().Be("https://github.com/login/oauth/authorize");
        query["client_id"].Should().Be("Iv1.client");
        Harness.StateProtector.Unprotect(query["state"]).Should().Be(CreateState());
    }

    [Fact]
    public async Task CompleteConnect_ShouldOfferTheInstallationsTheUserAdministersWhenNoneWasInstalled()
    {
        StubOAuthExchange();
        StubUserInstallations($"[{Installation}, {OtherOrganisationInstallation}, {SuspendedInstallation}]");
        StubMemberships(AdministeredMemberships);
        StubSignedInUser();

        var result = await Harness.Connector.CompleteConnect(
            CreateAuthorisationCallback(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Connection.Should().BeNull();

        var choice = result.Payload.Choice!;
        var candidate = choice.Candidates.Should().ContainSingle().Subject;
        var candidateState = Harness.StateProtector.Unprotect(candidate.CallbackParameters["state"]);

        candidate.ExternalAccountId.Should().Be(GitHubTestHarness.InstallationId);
        candidate.Account.Login.Should().Be("acme");
        candidate.CallbackParameters["installation_id"].Should().Be(GitHubTestHarness.InstallationId);
        candidateState!.UserAccessToken.Should().Be("ghu_user");

        var installUrl = new Uri(choice.InstallUrl);
        var installState = Harness.StateProtector.Unprotect(ParseQuery(installUrl)["state"]);

        installUrl.GetLeftPart(UriPartial.Path).Should().Be("https://github.com/apps/netptune-test/installations/new");
        installState.Should().Be(CreateState());
    }

    [Fact]
    public async Task CompleteConnect_ShouldReadMembershipsOnceForEveryOfferedInstallation()
    {
        StubOAuthExchange();
        StubUserInstallations($"[{Installation}, {OtherOrganisationInstallation}, {SuspendedInstallation}]");
        StubMemberships(AdministeredMemberships);
        StubSignedInUser();

        await Harness.Connector.CompleteConnect(CreateAuthorisationCallback(), TestContext.Current.CancellationToken);

        var membershipRequests = Harness.Http.Requests.Where(request => request.Uri.AbsolutePath.StartsWith("/user/memberships"));

        membershipRequests.Should().ContainSingle();
    }

    [Fact]
    public async Task CompleteConnect_ShouldOfferAPersonalAccountInstallationToItsOwner()
    {
        StubOAuthExchange();
        StubUserInstallations($"[{UserAccountInstallation}]");
        StubMemberships("[]");
        StubSignedInUser();

        var result = await Harness.Connector.CompleteConnect(
            CreateAuthorisationCallback(),
            TestContext.Current.CancellationToken);

        var candidate = result.Payload!.Choice!.Candidates.Should().ContainSingle().Subject;

        candidate.Account.Login.Should().Be("jo");
    }

    [Fact]
    public async Task CompleteConnect_ShouldFollowEveryPageOfTheUsersInstallations()
    {
        StubOAuthExchange();
        StubUserInstallations(
            $"[{OtherOrganisationInstallation}]",
            configure: response => response.Headers.Add("Link", $"<{SecondInstallationsPage}>; rel=\"next\""));

        Harness.Http.On(
            HttpMethod.Get,
            SecondInstallationsPage,
            HttpStatusCode.OK,
            $$"""{ "installations": [{{Installation}}] }""");

        StubMemberships(AdministeredMemberships);
        StubSignedInUser();

        var result = await Harness.Connector.CompleteConnect(
            CreateAuthorisationCallback(),
            TestContext.Current.CancellationToken);

        var candidate = result.Payload!.Choice!.Candidates.Should().ContainSingle().Subject;

        candidate.Account.Login.Should().Be("acme");
    }

    [Fact]
    public async Task CompleteConnect_ShouldOfferNoAccountsWhenTheUserHasNoInstallations()
    {
        StubOAuthExchange();
        StubUserInstallations("[]");
        StubMemberships(AdministeredMemberships);
        StubSignedInUser();

        var result = await Harness.Connector.CompleteConnect(
            CreateAuthorisationCallback(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Choice!.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteConnect_ShouldConnectAChosenInstallationWithoutAnotherCode()
    {
        StubInstallation(Installation);
        Harness.Http.On(HttpMethod.Get, AcmeMembershipUrl, HttpStatusCode.OK, AdminMembership);

        var authorisedState = Harness.StateProtector.Protect(CreateState() with { UserAccessToken = "ghu_user" });

        var callback = CreateCallback() with
        {
            Parameters = new Dictionary<string, string>
            {
                ["installation_id"] = GitHubTestHarness.InstallationId,
                ["state"] = authorisedState,
            },
        };

        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Connection!.Account.Login.Should().Be("acme");
        Harness.Http.Requests.Should().NotContain(request => request.Uri.AbsolutePath == "/login/oauth/access_token");
    }

    [Fact]
    public async Task CompleteConnect_ShouldStillRequireAdminForAChosenInstallation()
    {
        StubInstallation(Installation);
        Harness.Http.On(HttpMethod.Get, AcmeMembershipUrl, HttpStatusCode.OK, MemberMembership);

        var authorisedState = Harness.StateProtector.Protect(CreateState() with { UserAccessToken = "ghu_user" });

        var callback = CreateCallback() with
        {
            Parameters = new Dictionary<string, string>
            {
                ["installation_id"] = GitHubTestHarness.InstallationId,
                ["state"] = authorisedState,
            },
        };

        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Only an owner");
    }

    [Fact]
    public async Task CompleteConnect_ShouldConnectAnInstallationForAnOrganisationTheUserAdministers()
    {
        StubOAuthExchange();
        StubInstallation(Installation);
        Harness.Http.On(HttpMethod.Get, AcmeMembershipUrl, HttpStatusCode.OK, AdminMembership);

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        var connection = result.Payload!.Connection!;
        connection.ExternalAccountId.Should().Be(GitHubTestHarness.InstallationId);
        connection.Account.Login.Should().Be("acme");
        connection.Account.AccountType.Should().Be(ScmAccountType.Organisation);
        connection.CredentialKind.Should().Be(ScmCredentialKind.AppInstallation);
        connection.ProtectedCredential.Should().BeNull();
        connection.GrantedCapabilities.HasFlag(SourceControlCapabilities.ReadPullRequests).Should().BeTrue();

        var exchange = Harness.Http.Requests[0];
        exchange.Uri.AbsoluteUri.Should().Be("https://github.com/login/oauth/access_token");
        exchange.Accept.Should().Be("application/json");
        exchange.Body.Should().Contain("\"code\":\"oauth-code\"");
        var membershipRequest = Harness.Http.Requests.Single(request => request.Uri.AbsolutePath == AcmeMembershipUrl);
        membershipRequest.Authorization.Should().Be("Bearer ghu_user");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectAMemberWhoIsNotAnOrganisationAdmin()
    {
        StubOAuthExchange();
        StubInstallation(Installation);
        Harness.Http.On(HttpMethod.Get, AcmeMembershipUrl, HttpStatusCode.OK, MemberMembership);

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Only an owner");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectWhenGitHubRefusesToShowTheMembership()
    {
        StubOAuthExchange();
        StubInstallation(Installation);
        Harness.Http.On(HttpMethod.Get, AcmeMembershipUrl, HttpStatusCode.Forbidden, """{ "message": "Resource not accessible by integration" }""");

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Only an owner");
    }

    [Fact]
    public async Task CompleteConnect_ShouldConnectAUserAccountInstallationForItsOwner()
    {
        StubOAuthExchange();
        StubInstallation(UserAccountInstallation);
        Harness.Http.On(HttpMethod.Get, "/user", HttpStatusCode.OK, """{ "id": 42, "login": "jo" }""");

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Connection!.Account.Login.Should().Be("jo");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectAUserAccountInstallationForSomeoneElse()
    {
        StubOAuthExchange();
        StubInstallation(UserAccountInstallation);
        Harness.Http.On(HttpMethod.Get, "/user", HttpStatusCode.OK, """{ "id": 7, "login": "mallory" }""");

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Only an owner");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectStateStartedByAnotherUser()
    {
        var callback = CreateCallback() with { UserId = "someone-else" };

        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("someone else");
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectStateForAnotherWorkspace()
    {
        var callback = CreateCallback() with { WorkspaceId = WorkspaceId + 1 };

        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        Harness.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectTamperedState()
    {
        var callback = CreateCallback(state: "not-a-protected-state");

        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task CompleteConnect_ShouldReportAnInstallationAwaitingOwnerApproval()
    {
        var callback = CreateCallback(setupAction: "request");

        var result = await Harness.Connector.CompleteConnect(callback, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("approve");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectACodeGitHubDoesNotAccept()
    {
        Harness.Http.On(HttpMethod.Post, "https://github.com/login/oauth/access_token", HttpStatusCode.OK, """{ "error": "bad_verification_code" }""");

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("did not authorise");
    }

    public void Dispose()
    {
        Harness.Dispose();
    }

    private void StubInstallation(string installation)
    {
        Harness.Http.On(
            HttpMethod.Get,
            $"/app/installations/{GitHubTestHarness.InstallationId}", HttpStatusCode.OK, installation);
    }

    private void StubOAuthExchange()
    {
        Harness.Http.On(HttpMethod.Post, "https://github.com/login/oauth/access_token", HttpStatusCode.OK, """{ "access_token": "ghu_user", "token_type": "bearer" }""");
    }

    private void StubUserInstallations(string installations, Action<HttpResponseMessage>? configure = null)
    {
        Harness.Http.On(
            HttpMethod.Get,
            "/user/installations?per_page=100",
            HttpStatusCode.OK,
            $$"""{ "installations": {{installations}} }""",
            configure);
    }

    private void StubMemberships(string memberships)
    {
        Harness.Http.On(HttpMethod.Get, "/user/memberships/orgs?state=active&per_page=100", HttpStatusCode.OK, memberships);
    }

    private void StubSignedInUser()
    {
        Harness.Http.On(HttpMethod.Get, "/user", HttpStatusCode.OK, """{ "id": 42, "login": "jo" }""");
    }

    private static ScmConnectState CreateState()
    {
        return new ScmConnectState
        {
            Provider = SourceControlProvider.GitHub,
            WorkspaceId = WorkspaceId,
            UserId = UserId,
        };
    }

    private static Dictionary<string, string> ParseQuery(Uri uri)
    {
        var pairs = uri.Query.TrimStart('?').Split('&');

        return pairs
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));
    }

    private ScmConnectCallback CreateAuthorisationCallback()
    {
        return new ScmConnectCallback
        {
            WorkspaceId = WorkspaceId,
            UserId = UserId,
            Parameters = new Dictionary<string, string>
            {
                ["code"] = "oauth-code",
                ["state"] = Harness.StateProtector.Protect(CreateState()),
            },
        };
    }

    private ScmConnectCallback CreateCallback(string? state = null, string setupAction = "install")
    {
        var protectedState = state ?? Harness.StateProtector.Protect(CreateState());

        return new ScmConnectCallback
        {
            WorkspaceId = WorkspaceId,
            UserId = UserId,
            Parameters = new Dictionary<string, string>
            {
                ["installation_id"] = GitHubTestHarness.InstallationId,
                ["setup_action"] = setupAction,
                ["code"] = "oauth-code",
                ["state"] = protectedState,
            },
        };
    }
}
