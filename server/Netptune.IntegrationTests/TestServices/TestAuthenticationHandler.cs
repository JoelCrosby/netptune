using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

namespace Netptune.IntegrationTests.TestServices;

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly DataContext DataContext;

    public const string AuthenticationScheme = "TestScheme";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DataContext context)
        : base(options, logger, encoder)
    {
        DataContext = context;
    }

    public const string AnonymousHeader = "x-test-anonymous";

    // Signs the request in as the user with this email instead of the netptune Owner, so tests can
    // check what other members are denied.
    public const string UserHeader = "x-test-user";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var isAnonymousRequest = Request.Headers.ContainsKey(AnonymousHeader);

        if (isAnonymousRequest)
        {
            return AuthenticateResult.NoResult();
        }

        var hasRequestedUser = Request.Headers.TryGetValue(UserHeader, out var requestedEmail);
        var user = hasRequestedUser
            ? await DataContext.Users.FirstOrDefaultAsync(u => u.Email == requestedEmail.ToString())
            : await GetDefaultUser();

        if (user is null)
        {
            throw new InvalidOperationException("could not find user");
        }

        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, user.DisplayName),
            new (ClaimTypes.NameIdentifier, user.Id),
            new (ClaimTypes.Email, user.Email!),
            new (NetptuneClaims.ActorType, user.UserType.ToString()),
        };

        if (Request.Headers.TryGetValue("workspace", out var workspace))
        {
            claims.Add(new Claim(NetptuneClaims.Workspace, workspace!));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private Task<AppUser?> GetDefaultUser()
    {
        return DataContext.WorkspaceAppUsers
            .Include(u => u.Workspace)
            .Include(u => u.User)
            .Where(u => u.Workspace.Slug == "netptune" && u.User.UserType == AppUserType.User)
            .OrderBy(u => u.Role == WorkspaceRole.Owner ? 0 : 1)
            .ThenBy(u => u.UserId)
            .Select(u => u.User)
            .FirstOrDefaultAsync();
    }
}
