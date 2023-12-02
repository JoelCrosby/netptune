using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Authorization;
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

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = await DataContext.WorkspaceAppUsers
            .Include(u => u.Workspace)
            .Include(u => u.User)
            .Where(u => u.Workspace.Slug == "netptune")
            .Select(u => u.User)
            .FirstOrDefaultAsync();


        if (user is null)
        {
            throw new InvalidOperationException("could not find user");
        }

        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, user.DisplayName),
            new (ClaimTypes.NameIdentifier, user.Id),
            new (ClaimTypes.Email, user.Email!),
            new (NetptuneClaims.Workspace, "netptune"),
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
}
