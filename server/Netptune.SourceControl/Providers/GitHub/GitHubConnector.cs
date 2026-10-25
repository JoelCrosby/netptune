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

    // Starts with user authorisation rather than the install page, because GitHub sends an account that
    // already has the App installed to its settings page and never redirects back.
    public ScmConnectStart BeginConnect(ScmConnectRequest request)
    {
        var state = new ScmConnectState
        {
            Provider = SourceControlProvider.GitHub,
            WorkspaceId = request.WorkspaceId,
            UserId = request.UserId,
        };

        var webBaseUrl = Options.WebBaseUrl.TrimEnd('/');
        var clientId = Uri.EscapeDataString(Options.ClientId);
        var escapedState = Uri.EscapeDataString(StateProtector.Protect(state));

        return new ScmConnectStart($"{webBaseUrl}/login/oauth/authorize?client_id={clientId}&state={escapedState}");
    }

    public async ValueTask<ClientResponse<ScmConnectOutcome>> CompleteConnect(
        ScmConnectCallback callback,
        CancellationToken cancellationToken)
    {
        var parameters = callback.Parameters;
        var setupAction = parameters.GetValueOrDefault("setup_action");
        var isAwaitingApproval = setupAction == "request";

        if (isAwaitingApproval)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(AwaitingApproval);
        }

        var stateResult = ValidateState(callback);

        if (!stateResult.IsSuccess)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(stateResult.Message);
        }

        var state = stateResult.Payload!;
        var userToken = await ResolveUserToken(state, parameters, cancellationToken);

        if (userToken is null)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(NotAuthorised);
        }

        var installationIdValue = parameters.GetValueOrDefault("installation_id");

        if (installationIdValue is null)
        {
            var choice = await CreateChoice(state, userToken, cancellationToken);

            return ClientResponse<ScmConnectOutcome>.Success(ScmConnectOutcome.Choose(choice));
        }

        var hasInstallationId = long.TryParse(
            installationIdValue,
            CultureInfo.InvariantCulture,
            out var installationId);

        if (!hasInstallationId)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(MissingInstallation);
        }

        var connected = await ConnectInstallation(installationId, userToken, cancellationToken);

        if (!connected.IsSuccess)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(connected.Message);
        }

        return ClientResponse<ScmConnectOutcome>.Success(ScmConnectOutcome.Connected(connected.Payload!));
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

    private ClientResponse<ScmConnectState> ValidateState(ScmConnectCallback callback)
    {
        var protectedState = callback.Parameters.GetValueOrDefault("state");

        if (string.IsNullOrWhiteSpace(protectedState))
        {
            return ClientResponse<ScmConnectState>.Failed(MissingState);
        }

        var state = StateProtector.Unprotect(protectedState);

        if (state is null)
        {
            return ClientResponse<ScmConnectState>.Failed(ExpiredState);
        }

        var isGitHub = state.Provider == SourceControlProvider.GitHub;
        var isSameWorkspace = state.WorkspaceId == callback.WorkspaceId;
        var isSameUser = state.UserId == callback.UserId;
        var belongsToCaller = isGitHub && isSameWorkspace && isSameUser;

        if (!belongsToCaller)
        {
            return ClientResponse<ScmConnectState>.Failed(ForeignState);
        }

        return ClientResponse<ScmConnectState>.Success(state);
    }

    private async Task<string?> ResolveUserToken(
        ScmConnectState state,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        if (state.UserAccessToken is not null)
        {
            return state.UserAccessToken;
        }

        var code = parameters.GetValueOrDefault("code");

        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var userToken = await ExchangeCode(code, cancellationToken);

        return userToken;
    }

    private async Task<ClientResponse<ScmConnectResult>> ConnectInstallation(
        long installationId,
        string userToken,
        CancellationToken cancellationToken)
    {
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

    private async Task<ScmConnectChoice> CreateChoice(
        ScmConnectState state,
        string userToken,
        CancellationToken cancellationToken)
    {
        var installations = await ListAsUser<GitHubUserInstallationsDto, GitHubInstallationDto>(
            userToken,
            "/user/installations?per_page=100",
            page => page.Installations,
            cancellationToken);

        var administeredAccountIds = await GetAdministeredAccountIds(userToken, cancellationToken);
        var authorisedState = StateProtector.Protect(state with { UserAccessToken = userToken });

        var candidates = installations
            .Where(installation => installation.SuspendedAt is null)
            .Where(installation => administeredAccountIds.Contains(installation.Account.Id))
            .Select(installation => ToCandidate(installation, authorisedState))
            .ToList();

        // The token stays out of the install URL; GitHub returns a fresh code after an install.
        var installState = StateProtector.Protect(state with { UserAccessToken = null });

        return new ScmConnectChoice
        {
            Candidates = candidates,
            InstallUrl = CreateInstallUrl(installState),
        };
    }

    private async Task<HashSet<long>> GetAdministeredAccountIds(string userToken, CancellationToken cancellationToken)
    {
        var memberships = await ListAsUser<List<GitHubOrganisationMembershipDto>, GitHubOrganisationMembershipDto>(
            userToken,
            "/user/memberships/orgs?state=active&per_page=100",
            page => page,
            cancellationToken);

        var administeredOrganisationIds = memberships
            .Where(IsActiveAdmin)
            .Select(membership => membership.Organization.Id);

        var user = await SendAsUser<GitHubAccountDto>(userToken, "/user", cancellationToken);
        long[] ownAccountIds = user is null ? [] : [user.Id];

        return [.. administeredOrganisationIds, .. ownAccountIds];
    }

    private static ScmConnectCandidate ToCandidate(GitHubInstallationDto installation, string authorisedState)
    {
        var installationId = GitHubMapper.ToExternalId(installation.Id);

        return new ScmConnectCandidate
        {
            ExternalAccountId = installationId,
            Account = ToAccount(installation.Account),
            CallbackParameters = new Dictionary<string, string>
            {
                ["installation_id"] = installationId,
                ["state"] = authorisedState,
            },
        };
    }

    private string CreateInstallUrl(string protectedState)
    {
        var webBaseUrl = Options.WebBaseUrl.TrimEnd('/');
        var appSlug = Uri.EscapeDataString(Options.AppSlug);
        var escapedState = Uri.EscapeDataString(protectedState);

        return $"{webBaseUrl}/apps/{appSlug}/installations/new?state={escapedState}";
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
        var isActiveAdmin = membership is not null && IsActiveAdmin(membership);

        return isActiveAdmin;
    }

    private static bool IsActiveAdmin(GitHubOrganisationMembershipDto membership)
    {
        var isActive = membership.State == "active";
        var isAdmin = membership.Role == "admin";

        return isActive && isAdmin;
    }

    private async Task<List<TItem>> ListAsUser<TPage, TItem>(
        string userToken,
        string firstPageUrl,
        Func<TPage, IEnumerable<TItem>> readItems,
        CancellationToken cancellationToken)
        where TPage : class
    {
        var items = new List<TItem>();
        string? url = firstPageUrl;

        while (url is not null)
        {
            var response = await SendAsUserForResponse<TPage>(userToken, url, cancellationToken);

            if (response?.Payload is null)
            {
                break;
            }

            items.AddRange(readItems(response.Payload));
            url = response.NextPageUrl;
        }

        return items;
    }

    private async Task<T?> SendAsUser<T>(string userToken, string url, CancellationToken cancellationToken)
        where T : class
    {
        var response = await SendAsUserForResponse<T>(userToken, url, cancellationToken);

        return response?.Payload;
    }

    // A 403 here means the user (or the app's Members permission) can't read the membership, which
    // is a refusal to connect rather than a provider failure.
    private async Task<GitHubResponse<T>?> SendAsUserForResponse<T>(
        string userToken,
        string url,
        CancellationToken cancellationToken)
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

            return response;
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
