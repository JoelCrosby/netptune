using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Authorization;
using Netptune.Core.Authentication;
using Netptune.Core.UnitOfWork;

namespace Netptune.Identity.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string HeaderPrefix = "ApiKey ";
    private readonly INetptuneUnitOfWork UnitOfWork;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        INetptuneUnitOfWork unitOfWork)
        : base(options, logger, encoder)
    {
        UnitOfWork = unitOfWork;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization[HeaderPrefix.Length..].Trim();

        if (!ApiCredentialToken.TryParse(token, out var credentialId, out var presentedHash))
        {
            return AuthenticateResult.Fail("Invalid API credential.");
        }

        var credential = await UnitOfWork.ServiceAccounts.GetCredentialAuthentication(credentialId, Context.RequestAborted);
        var now = DateTime.UtcNow;

        if (credential is null
            || credential.RevokedAt.HasValue
            || credential.DisabledAt.HasValue
            || credential.ExpiresAt <= now
            || credential.SecretHash.Length != presentedHash.Length
            || !CryptographicOperations.FixedTimeEquals(credential.SecretHash, presentedHash))
        {
            return AuthenticateResult.Fail("Invalid API credential.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, credential.UserId),
            new(ClaimTypes.Name, credential.DisplayName),
            new(NetptuneClaims.Workspace, credential.WorkspaceKey),
            new(NetptuneClaims.CredentialId, credential.CredentialId.ToString("N")),
            new(NetptuneClaims.ActorType, AppUserType.ServiceAccount.ToString()),
        };

        claims.AddRange(credential.Scopes.Select(scope => new Claim(NetptuneClaims.CredentialScope, scope)));

        var identity = new ClaimsIdentity(claims, AuthenticationSchemes.ApiKey);
        var principal = new ClaimsPrincipal(identity);

        if (!credential.LastUsedAt.HasValue || credential.LastUsedAt.Value < now.AddMinutes(-15))
        {
            await UnitOfWork.ServiceAccounts.TouchCredential(credential.CredentialId, now, Context.RequestAborted);
        }

        return AuthenticateResult.Success(new AuthenticationTicket(principal, AuthenticationSchemes.ApiKey));
    }
}
