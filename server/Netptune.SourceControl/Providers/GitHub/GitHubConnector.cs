using System.Globalization;
using System.Net;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.SourceControl.Connect;
using Netptune.SourceControl.Providers.GitHub.Dtos;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubConnector : ISourceControlConnector
{
    private const string AwaitingApproval = "The installation is waiting for an organisation owner to approve it on GitHub.";

    private const string MissingInstallation = "GitHub did not return an installation to connect. Start again.";

    private const string NotAuthorised = "GitHub did not authorise the connection. Start again.";

    private const string NotInstallationAdmin = "Only an owner of the GitHub organisation or account that installed the app can connect it.";

    private const string InstallationNotFound = "GitHub could not find that installation.";

    private const string InstallationSuspended = "That GitHub installation is suspended. Unsuspend it on GitHub, then try again.";

    private const string MissingState = "The connection request is missing its state. Start again.";

    private const string ExpiredState = "The connection request has expired. Start again.";

    private const string ForeignState = "The connection request was started by someone else. Start again.";

    // Refresh well before expiry so a token handed to a long backfill doesn't lapse mid-request.
    private static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromMinutes(10);

    private readonly GitHubApi Api;
    private readonly GitHubAppTokenMinter TokenMinter;
    private readonly ScmConnectStateProtector StateProtector;
    private readonly IMemoryCache Cache;
    private readonly GitHubAppOptions Options;
    private readonly TimeProvider Clock;

    public GitHubConnector(
        GitHubApi api,
        GitHubAppTokenMinter tokenMinter,
        ScmConnectStateProtector stateProtector,
        IMemoryCache cache,
        IOptions<GitHubAppOptions> options,
        TimeProvider clock)
    {
        Api = api;
        TokenMinter = tokenMinter;
        StateProtector = stateProtector;
        Cache = cache;
        Options = options.Value;
        Clock = clock;
    }

    public ScmConnectStart BeginConnect(ScmConnectRequest request)
    {
        var state = new ScmConnectState(SourceControlProvider.GitHub, request.WorkspaceId, request.UserId);
        var protectedState = StateProtector.Protect(state);
        var webBaseUrl = Options.WebBaseUrl.TrimEnd('/');
        var appSlug = Uri.EscapeDataString(Options.AppSlug);
        var escapedState = Uri.EscapeDataString(protectedState);

        return new ScmConnectStart($"{webBaseUrl}/apps/{appSlug}/installations/new?state={escapedState}");
    }

    public async ValueTask<ClientResponse<ScmConnectResult>> CompleteConnect(
        ScmConnectCallback callback,
        CancellationToken cancellationToken)
    {
        var parameters = callback.Parameters;
        var setupAction = parameters.GetValueOrDefault("setup_action");
        var isAwaitingApproval = setupAction == "request";

        if (isAwaitingApproval)
        {
            return ClientResponse<ScmConnectResult>.Failed(AwaitingApproval);
        }

        var stateResult = ValidateState(callback);

        if (!stateResult.IsSuccess)
        {
            return ClientResponse<ScmConnectResult>.Failed(stateResult.Message);
        }

        var installationIdValue = parameters.GetValueOrDefault("installation_id");
        var hasInstallationId = long.TryParse(
            installationIdValue,
            CultureInfo.InvariantCulture,
            out var installationId);

        var code = parameters.GetValueOrDefault("code");

        if (!hasInstallationId || string.IsNullOrWhiteSpace(code))
        {
            return ClientResponse<ScmConnectResult>.Failed(MissingInstallation);
        }

        var userToken = await ExchangeCode(code, cancellationToken);

        if (userToken is null)
        {
            return ClientResponse<ScmConnectResult>.Failed(NotAuthorised);
        }

        var installation = await GetInstallation(installationId, cancellationToken);

        if (installation is null)
        {
            return ClientResponse<ScmConnectResult>.Failed(InstallationNotFound);
        }

        // GitHub's setup URL accepts any installation_id in the query string, and any member who can
        // read one of its repositories can see an installation, so only its account's owners may attach it.
        var administersAccount = await UserAdministersAccount(userToken, installation.Account, cancellationToken);

        if (!administersAccount)
        {
            return ClientResponse<ScmConnectResult>.Failed(NotInstallationAdmin);
        }

        var isSuspended = installation.SuspendedAt is not null;

        if (isSuspended)
        {
            return ClientResponse<ScmConnectResult>.Failed(InstallationSuspended);
        }

        var result = new ScmConnectResult
        {
            ExternalAccountId = GitHubMapper.ToExternalId(installation.Id),
            Account = ToAccount(installation.Account),
            CredentialKind = ScmCredentialKind.AppInstallation,
            GrantedCapabilities = GitHubPermissions.ToCapabilities(installation.Permissions),
        };

        return ClientResponse<ScmConnectResult>.Success(result);
    }

    public async ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken)
    {
        var cacheKey = $"scm:github:installation-token:{connection.ExternalAccountId}";
        var isCached = Cache.TryGetValue<ScmAccessToken>(cacheKey, out var cached);
        var refreshAfter = Clock.GetUtcNow() + TokenRefreshMargin;

        if (isCached && cached!.ExpiresAt > refreshAfter)
        {
            return cached;
        }

        var installationId = Uri.EscapeDataString(connection.ExternalAccountId);

        var request = new GitHubRequest
        {
            Method = HttpMethod.Post,
            Url = $"/app/installations/{installationId}/access_tokens",
            BearerToken = TokenMinter.CreateAppJwt(),
        };

        var response = await Api.Send<GitHubAccessTokenDto>(request, cancellationToken);

        if (!response.IsSuccess || response.Payload is null)
        {
            throw new ScmProviderException(
                SourceControlProvider.GitHub,
                (int)response.StatusCode,
                response.ErrorMessage);
        }

        var token = new ScmAccessToken(response.Payload.Token, response.Payload.ExpiresAt);
        var cacheLifetime = token.ExpiresAt - TokenRefreshMargin - Clock.GetUtcNow();

        if (cacheLifetime > TimeSpan.Zero)
        {
            Cache.Set(cacheKey, token, cacheLifetime);
        }

        return token;
    }

    public async ValueTask<SourceControlCapabilities> GetGrantedCapabilities(
        ScmConnection connection,
        CancellationToken cancellationToken)
    {
        var hasInstallationId = long.TryParse(
            connection.ExternalAccountId,
            CultureInfo.InvariantCulture,
            out var installationId);

        if (!hasInstallationId)
        {
            return SourceControlCapabilities.None;
        }

        var installation = await GetInstallation(installationId, cancellationToken);

        if (installation is null)
        {
            return SourceControlCapabilities.None;
        }

        return GitHubPermissions.ToCapabilities(installation.Permissions);
    }

    private ClientResponse ValidateState(ScmConnectCallback callback)
    {
        var protectedState = callback.Parameters.GetValueOrDefault("state");

        if (string.IsNullOrWhiteSpace(protectedState))
        {
            return ClientResponse.Failed(MissingState);
        }

        var state = StateProtector.Unprotect(protectedState);

        if (state is null)
        {
            return ClientResponse.Failed(ExpiredState);
        }

        var isGitHub = state.Provider == SourceControlProvider.GitHub;
        var isSameWorkspace = state.WorkspaceId == callback.WorkspaceId;
        var isSameUser = state.UserId == callback.UserId;
        var belongsToCaller = isGitHub && isSameWorkspace && isSameUser;

        if (!belongsToCaller)
        {
            return ClientResponse.Failed(ForeignState);
        }

        return ClientResponse.Success;
    }

    private async Task<string?> ExchangeCode(string code, CancellationToken cancellationToken)
    {
        var webBaseUrl = Options.WebBaseUrl.TrimEnd('/');

        var request = new GitHubRequest
        {
            Method = HttpMethod.Post,
            Url = $"{webBaseUrl}/login/oauth/access_token",
            Accept = "application/json",
            Body = new GitHubOAuthTokenRequestDto
            {
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                Code = code,
            },
        };

        var response = await Api.Send<GitHubOAuthTokenDto>(request, cancellationToken);
        var accessToken = response.Payload?.AccessToken;

        return string.IsNullOrWhiteSpace(accessToken) ? null : accessToken;
    }

    private async Task<bool> UserAdministersAccount(
        string userToken,
        GitHubAccountDto account,
        CancellationToken cancellationToken)
    {
        var isOrganisation = string.Equals(account.Type, "Organization", StringComparison.OrdinalIgnoreCase);

        if (isOrganisation)
        {
            var isOrganisationAdmin = await UserIsOrganisationAdmin(userToken, account.Login, cancellationToken);

            return isOrganisationAdmin;
        }

        var user = await SendAsUser<GitHubAccountDto>(userToken, "/user", cancellationToken);
        var isAccountOwner = user is not null && user.Id == account.Id;

        return isAccountOwner;
    }

    private async Task<bool> UserIsOrganisationAdmin(
        string userToken,
        string organisation,
        CancellationToken cancellationToken)
    {
        var url = $"/user/memberships/orgs/{Uri.EscapeDataString(organisation)}";
        var membership = await SendAsUser<GitHubOrganisationMembershipDto>(userToken, url, cancellationToken);

        var isActive = membership?.State == "active";
        var isAdmin = membership?.Role == "admin";

        return isActive && isAdmin;
    }

    // A 403 here means the user (or the app's Members permission) can't read the membership, which
    // is a refusal to connect rather than a provider failure.
    private async Task<T?> SendAsUser<T>(string userToken, string url, CancellationToken cancellationToken)
        where T : class
    {
        var request = new GitHubRequest
        {
            Method = HttpMethod.Get,
            Url = url,
            BearerToken = userToken,
        };

        try
        {
            var response = await Api.Send<T>(request, cancellationToken);

            return response.Payload;
        }
        catch (ScmProviderException exception) when (exception.StatusCode == (int)HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    private async Task<GitHubInstallationDto?> GetInstallation(long installationId, CancellationToken cancellationToken)
    {
        var request = new GitHubRequest
        {
            Method = HttpMethod.Get,
            Url = $"/app/installations/{installationId.ToString(CultureInfo.InvariantCulture)}",
            BearerToken = TokenMinter.CreateAppJwt(),
        };

        var response = await Api.Send<GitHubInstallationDto>(request, cancellationToken);

        return response.Payload;
    }

    private static ScmAccount ToAccount(GitHubAccountDto account)
    {
        var accountType = account.Type == "Organization" ? ScmAccountType.Organisation : ScmAccountType.User;

        return new ScmAccount
        {
            ExternalId = GitHubMapper.ToExternalId(account.Id),
            Login = account.Login,
            AccountType = accountType,
            AvatarUrl = account.AvatarUrl,
        };
    }
}
