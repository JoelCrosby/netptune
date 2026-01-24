using System.Threading.Tasks;

using Flurl;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Services.Authentication;

namespace Netptune.App.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("auth");

        group.MapPost("/login", HandleLogin).AllowAnonymous();
        group.MapPost("/register", HandleRegister).AllowAnonymous();
        group.MapGet("/confirm-email", HandleConfirmEmail).AllowAnonymous();
        group.MapGet("/request-password-reset", HandleRequestPasswordReset).AllowAnonymous();
        group.MapGet("/reset-password", HandleResetPassword).AllowAnonymous();
        group.MapPatch("/change-password", HandleChangePassword).RequireAuthorization();
        group.MapGet("/current-user", HandleCurrentUser).AllowAnonymous();
        group.MapGet("/validate-workspace-invite", HandleValidateWorkspaceInvite).AllowAnonymous();
        group.MapGet("/github-login", HandleGithubLogin).AllowAnonymous();
        group.MapGet("/provider-login-redirect", HandleProviderLoginRedirect)
            .RequireAuthorization(AuthenticationSchemes.Github);
        group.MapPost("/provider-login-redirect", HandleProviderLoginRedirect)
            .RequireAuthorization(AuthenticationSchemes.Github);

        return group;
    }

    public static async Task<IResult> HandleLogin(
        INetptuneAuthService authenticationService,
        TokenRequest request)
    {
        var result = await authenticationService.LogIn(request);

        if (!result.IsSuccess) return Results.Unauthorized();

        return Results.Ok(result.Ticket);
    }

    public static async Task<IResult> HandleRegister(
        INetptuneAuthService authenticationService,
        RegisterRequest request)
    {
        var result = await authenticationService.Register(request);

        if (!result.IsSuccess) {
            return Results.Problem(
                detail: result.Message ?? "Unauthorized",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        return Results.Ok(result.Ticket);
    }

    public static async Task<IResult> HandleConfirmEmail(
        INetptuneAuthService authenticationService,
        string userId,
        string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return Results.Unauthorized();
        }

        // Encoding for plus symbols is going wonky some where ...
        var decodedCode = code.Replace(' ', '+');

        var result = await authenticationService.ConfirmEmail(userId, decodedCode);

        if (!result.IsSuccess) return Results.Unauthorized();

        return Results.Ok(result.Ticket);
    }

    public static async Task<IResult> HandleRequestPasswordReset(
        INetptuneAuthService authenticationService,
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.BadRequest();
        }

        var result = await authenticationService.RequestPasswordReset(new RequestPasswordResetRequest
        {
            Email = email,
        });

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleResetPassword(
        INetptuneAuthService authenticationService,
        string userId,
        string code,
        string password)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(password))
        {
            return Results.Unauthorized();
        }

        // Encoding for plus symbols is going wonky some where ...
        var decodedCode = code.Replace(' ', '+');

        var result = await authenticationService.ResetPassword(new ResetPasswordRequest
        {
            Code = decodedCode,
            UserId = userId,
            Password = password,
        });

        if (!result.IsSuccess) return Results.Unauthorized();

        return Results.Ok(result.Ticket);
    }

    public static async Task<IResult> HandleChangePassword(
        INetptuneAuthService authenticationService,
        ChangePasswordRequest request)
    {
        var result = await authenticationService.ChangePassword(request);

        if (!result.IsSuccess) return Results.Unauthorized();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleCurrentUser(
        INetptuneAuthService authenticationService)
    {
        var result = await authenticationService.CurrentUser();

        if (result is null) return Results.Unauthorized();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleValidateWorkspaceInvite(
        INetptuneAuthService authenticationService,
        string code)
    {
        var result = await authenticationService.ValidateInviteCode(code);

        if (result is null) return Results.Unauthorized();

        return Results.Ok(result);
    }

    public static IResult HandleGithubLogin(HttpContext context)
    {
        return Results.Challenge(new AuthenticationProperties
        {
            RedirectUri = "/api/auth/provider-login-redirect",
            IsPersistent = true,
        }, new[] { "GitHub" });
    }

    public static async Task<IResult> HandleProviderLoginRedirect(
        INetptuneAuthService authenticationService,
        IHostingService hosting,
        HttpContext context)
    {
        var result = await authenticationService.LogInViaProvider();

        if (!result.IsSuccess) return Results.Unauthorized();

        var redirect = hosting.ClientOrigin
            .AppendPathSegments("/auth/auth-provider-login")
            .SetQueryParams(new
            {
                expires = result.Ticket?.Expires,
                issued = result.Ticket?.Issued,
                token = result.Ticket?.Token,
                displayName = result.Ticket?.DisplayName,
                email = result.Ticket?.EmailAddress,
                pictureUrl = result.Ticket?.PictureUrl,
                userId = result.Ticket?.UserId,
            });

        return Results.Redirect(redirect);
    }
}
