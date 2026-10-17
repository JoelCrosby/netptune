using System.Net;

using FluentAssertions;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.SourceControl.Connect;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public sealed class GitHubConnectorTests : IDisposable
{
    private const int WorkspaceId = 3;
    private const string UserId = "user-1";

    private const string UserInstallations = """
        { "total_count": 1, "installations": [ { "id": 987654, "account": { "id": 11, "login": "acme", "type": "Organization" } } ] }
        """;

    private const string OtherUserInstallations = """
        { "total_count": 1, "installations": [ { "id": 1111, "account": { "id": 12, "login": "elsewhere", "type": "User" } } ] }
        """;

    private const string Installation = """
        {
          "id": 987654,
          "account": { "id": 11, "login": "acme", "type": "Organization", "avatar_url": "https://avatars.example/acme" },
          "permissions": { "metadata": "read", "contents": "read", "pull_requests": "read", "checks": "read" },
          "suspended_at": null
        }
        """;

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

        var getToken = async () => await Harness.Connector.GetAccessToken(connection, TestContext.Current.CancellationToken);

        await getToken.Should().ThrowAsync<ScmProviderException>();
    }

    [Fact]
    public void BeginConnect_ShouldSendTheBrowserToTheAppInstallPageWithSignedState()
    {
        var start = Harness.Connector.BeginConnect(new ScmConnectRequest(WorkspaceId, UserId));

        var redirect = new Uri(start.RedirectUrl);
        var state = Uri.UnescapeDataString(redirect.Query["?state=".Length..]);

        redirect.GetLeftPart(UriPartial.Path).Should().Be("https://github.com/apps/netptune-test/installations/new");
        Harness.StateProtector.Unprotect(state).Should().Be(new ScmConnectState(SourceControlProvider.GitHub, WorkspaceId, UserId));
    }

    [Fact]
    public async Task CompleteConnect_ShouldConnectAnInstallationTheUserCanAccess()
    {
        StubOAuthExchange();
        Harness.Http.On(HttpMethod.Get, "/user/installations?per_page=100", HttpStatusCode.OK, UserInstallations);
        Harness.Http.On(HttpMethod.Get, $"/app/installations/{GitHubTestHarness.InstallationId}", HttpStatusCode.OK, Installation);

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.ExternalAccountId.Should().Be(GitHubTestHarness.InstallationId);
        result.Payload.Account.Login.Should().Be("acme");
        result.Payload.Account.AccountType.Should().Be(ScmAccountType.Organisation);
        result.Payload.CredentialKind.Should().Be(ScmCredentialKind.AppInstallation);
        result.Payload.ProtectedCredential.Should().BeNull();
        result.Payload.GrantedCapabilities.HasFlag(SourceControlCapabilities.ReadPullRequests).Should().BeTrue();
        result.Payload.GrantedCapabilities.HasFlag(SourceControlCapabilities.CreateBranch).Should().BeFalse("contents is read-only");

        var exchange = Harness.Http.Requests[0];
        exchange.Uri.AbsoluteUri.Should().Be("https://github.com/login/oauth/access_token");
        exchange.Accept.Should().Be("application/json");
        exchange.Body.Should().Contain("\"code\":\"oauth-code\"");
        Harness.Http.Requests[1].Authorization.Should().Be("Bearer ghu_user");
    }

    [Fact]
    public async Task CompleteConnect_ShouldRejectAnInstallationTheUserCannotSee()
    {
        StubOAuthExchange();
        Harness.Http.On(HttpMethod.Get, "/user/installations?per_page=100", HttpStatusCode.OK, OtherUserInstallations);

        var result = await Harness.Connector.CompleteConnect(CreateCallback(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("doesn't have access");
        Harness.Http.Requests.Should().NotContain(request => request.Uri.AbsolutePath.StartsWith("/app/installations"));
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

    private void StubOAuthExchange()
    {
        Harness.Http.On(HttpMethod.Post, "https://github.com/login/oauth/access_token", HttpStatusCode.OK, """{ "access_token": "ghu_user", "token_type": "bearer" }""");
    }

    private ScmConnectCallback CreateCallback(string? state = null, string setupAction = "install")
    {
        var connectState = new ScmConnectState(SourceControlProvider.GitHub, WorkspaceId, UserId);
        var protectedState = state ?? Harness.StateProtector.Protect(connectState);

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
