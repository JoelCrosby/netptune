using Flurl;

using Microsoft.AspNetCore.Authentication;

using Netptune.App.Utility;
using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Services.Authentication;

namespace Netptune.App.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("auth");

        group.MapPost("/login", HandleLogin).AllowAnonymous();
        group.MapPost("/register", HandleRegister).AllowAnonymous();
        group.MapPost("/confirm-email", HandleConfirmEmail).AllowAnonymous();
        group.MapGet("/request-password-reset", HandleRequestPasswordReset).AllowAnonymous();
        group.MapPost("/reset-password", HandleResetPassword).AllowAnonymous();
        group.MapPatch("/change-password", HandleChangePassword).RequireAuthorization();
        group.MapGet("/current-user", HandleCurrentUser).AllowAnonymous();
        group.MapGet("/validate-workspace-invite", HandleValidateWorkspaceInvite).AllowAnonymous();
        group.MapPost("/refresh", HandleRefresh).AllowAnonymous();
        group.MapPost("/logout", HandleLogout).RequireAuthorization();
        group.MapGet("/github-login", HandleGithubLogin).AllowAnonymous();
        group.MapGet("/provider-login-redirect", HandleProviderLoginRedirect)
            .RequireAuthorization(AuthenticationSchemes.Github);
        group.MapPost("/provider-login-redirect", HandleProviderLoginRedirect)
            .RequireAuthorization(AuthenticationSchemes.Github);

        return builder;
    }

    public static async Task<IResult> HandleLogin(
        INetptuneAuthService authenticationService,
        ITurnstileService turnstileService,
        HttpContext context,
        TokenRequest request)
    {
        var remoteIp = context.GetRemoteIpAddress();
        var success = await turnstileService.ValidateAsync(request.Turnstile, remoteIp);

        if (!success)
        {
            return Results.Unauthorized();
        }

        var result = await authenticationService.LogIn(request);

        if (!result.IsSuccess)
        {
            return Results.Unauthorized();
        }

        CookieHelper.SetAuthCookies(context.Response, result.Ticket!);

        return Results.Ok(result.Ticket!.ToUserResponse());
    }

    public static async Task<IResult> HandleRefresh(
        INetptuneAuthService authenticationService,
        HttpContext context)
    {
        var refreshToken = context.Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Results.Unauthorized();
        }

        var result = await authenticationService.Refresh(new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
        });

        if (!result.IsSuccess)
        {
            return Results.Unauthorized();
        }

        CookieHelper.SetAuthCookies(context.Response, result.Ticket!);

        return Results.Ok(result.Ticket!.ToUserResponse());
    }

    public static async Task<IResult> HandleRegister(
        INetptuneAuthService authenticationService,
        ITurnstileService turnstileService,
        HttpContext context,
        RegisterRequest request)
    {
        var remoteIp = context.GetRemoteIpAddress();
        var success = await turnstileService.ValidateAsync(request.Turnstile, remoteIp);

        if (!success)
        {
            return Results.Unauthorized();
        }

        var result = await authenticationService.Register(request);

        if (!result.IsSuccess) {
            return Results.Problem(
                detail: result.Message ?? "Unauthorized",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        CookieHelper.SetAuthCookies(context.Response, result.Ticket!);

        return Results.Ok(result.Ticket!.ToUserResponse());
    }

    public static async Task<IResult> HandleConfirmEmail(
        INetptuneAuthService authenticationService,
        HttpContext context,
        AuthCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.userId) || string.IsNullOrWhiteSpace(request.code))
        {
            return Results.Unauthorized();
        }

        // Encoding for plus symbols is going wonky
        var decodedCode = request.code.Replace(' ', '+');

        var result = await authenticationService.ConfirmEmail(request.userId, decodedCode);

        if (!result.IsSuccess) return Results.Unauthorized();

        CookieHelper.SetAuthCookies(context.Response, result.Ticket!);

        return Results.Ok(result.Ticket!.ToUserResponse());
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
        HttpContext context,
        ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId)
            || string.IsNullOrWhiteSpace(request.Code)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Unauthorized();
        }

        // Encoding for plus symbols is going wonky
        var decodedCode = request.Code.Replace(' ', '+');

        var result = await authenticationService.ResetPassword(new ResetPasswordRequest
        {
            Code = decodedCode,
            UserId = request.UserId,
            Password = request.Password,
        });

        if (!result.IsSuccess) return Results.Unauthorized();

        CookieHelper.SetAuthCookies(context.Response, result.Ticket!);

        return Results.Ok(result.Ticket!.ToUserResponse());
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

    public static IResult HandleLogout(HttpContext context)
    {
        CookieHelper.ClearAuthCookies(context.Response);
        return Results.Ok();
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

        CookieHelper.SetAuthCookies(context.Response, result.Ticket!);

        var redirect = hosting.ClientOrigin
            .AppendPathSegments("/auth/auth-provider-login")
            .SetQueryParams(new
            {
                displayName = result.Ticket?.DisplayName,
                email = result.Ticket?.EmailAddress,
                pictureUrl = result.Ticket?.PictureUrl,
                userId = result.Ticket?.UserId,
                expires = result.Ticket?.Expires.ToUniversalTime().ToString("O"),
            });

        return Results.Redirect(redirect);
    }
}
