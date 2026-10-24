using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Extensions;
using Netptune.Core.Services;

namespace Netptune.Identity.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    private const string GithubEmailsUrl = "https://api.github.com/user/emails";

    public static IdentityBuilder AddNetptuneIdentity(this IServiceCollection services)
    {
        return services.AddIdentity<AppUser, IdentityRole>()
            .AddDefaultTokenProviders();
    }

    public static IServiceCollection AddNetptuneAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(NetptuneAuthenticationOptions.SectionName);

        services.AddOptions<NetptuneAuthenticationOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The scheme callbacks below run lazily, after startup validation has rejected missing values.
        var authenticationOptions = section.Get<NetptuneAuthenticationOptions>() ?? new NetptuneAuthenticationOptions();
        var issuer = configuration.GetRequiredValue("Tokens:Issuer");
        var audience = configuration.GetRequiredValue("Tokens:Audience");

        services.AddHttpContextAccessor();
        services.AddHttpClient();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 8;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
        });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = AuthenticationSchemes.Smart;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })

        .AddPolicyScheme(AuthenticationSchemes.Smart, AuthenticationSchemes.Smart, options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Headers.Authorization.ToString().StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase)
                    ? AuthenticationSchemes.ApiKey
                    : JwtBearerDefaults.AuthenticationScheme;
        })

        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(AuthenticationSchemes.ApiKey, _ => { })

        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationOptions.SigningKey)),
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    string? workspace = null;

                    if (context.Request.Headers.TryGetValue(NetptuneClaims.Workspace, out var headerWorkspace))
                    {
                        workspace = headerWorkspace;
                    }
                    else if (context.Request.Path.StartsWithSegments("/api/hubs"))
                    {
                        workspace = context.Request.Query[NetptuneClaims.Workspace];
                    }

                    if (string.IsNullOrEmpty(workspace))
                    {
                        return Task.CompletedTask;
                    }

                    var claims = new[] { new Claim(NetptuneClaims.Workspace, workspace) };
                    var identity = new ClaimsIdentity(claims);

                    context.Principal?.AddIdentity(identity);

                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken)
                        && !string.IsNullOrEmpty(cookieToken))
                    {
                        context.Token = cookieToken;
                    }

                    return Task.CompletedTask;
                },
            };
        })

        .AddGitHub(options =>
        {
            options.ClientId = authenticationOptions.GitHub.ClientId;
            options.ClientSecret = authenticationOptions.GitHub.ClientSecret;
            options.CallbackPath = authenticationOptions.GitHub.CallbackPath;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.Scope.Add("read:user");
            options.Scope.Add("urn:github:name");
            options.Scope.Add("user:email");
            options.SaveTokens = true;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var original = context.RedirectUri;
                var redirect = original.Contains("6400") ? context.RedirectUri.Replace("6400", "7401") : original;

                context.Response.Redirect(redirect);

                return Task.CompletedTask;
            };
            options.Events.OnCreatingTicket = async context =>
            {
                var token = context.AccessToken;
                var factory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient();

                client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
                client.DefaultRequestHeaders.Add("User-Agent", "Netptune API");

                var user = await client.GetFromJsonAsync<GithubUserResponse>("https://api.github.com/user");

                if (user is not null)
                {
                    context.Identity?.AddClaim(new Claim("Provider-Picture-Url", user.AvatarUrl.ToString()));
                }

                var emails = await client.GetFromJsonAsync<List<GithubEmailResponse>>(GithubEmailsUrl);
                var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
                var isEmailVerified = emails?.Any(entry => entry.Verified && IsSameEmail(entry.Email, email)) ?? false;

                MarkEmailVerified(context.Identity, isEmailVerified);
            };
        });

        services.AddAuthentication()
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authenticationOptions.Google.ClientId;
                options.ClientSecret = authenticationOptions.Google.ClientSecret;
                options.CallbackPath = authenticationOptions.Google.CallbackPath;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = true;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events.OnCreatingTicket = context =>
                {
                    var pictureUrl = context.User.GetProperty("picture").GetString();

                    if (pictureUrl is not null)
                    {
                        context.Identity?.AddClaim(new Claim("Provider-Picture-Url", pictureUrl));
                    }

                    var hasVerifiedFlag = context.User.TryGetProperty("email_verified", out var emailVerified);
                    var isEmailVerified = hasVerifiedFlag && emailVerified.ValueKind == JsonValueKind.True;

                    MarkEmailVerified(context.Identity, isEmailVerified);

                    return Task.CompletedTask;
                };
            })
            .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authenticationOptions.Microsoft.ClientId;
                options.ClientSecret = authenticationOptions.Microsoft.ClientSecret;
                options.CallbackPath = authenticationOptions.Microsoft.CallbackPath;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = true;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events.OnCreatingTicket = context =>
                {
                    // Graph's mail is free text a tenant admin can set to anyone's address. A tenant can only
                    // give out sign-in names on domains it has verified, so only an email matching it is trusted.
                    var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
                    var hasSignInName = context.User.TryGetProperty("userPrincipalName", out var signInName);
                    var userPrincipalName = hasSignInName ? signInName.GetString() : null;
                    var isGuestName = userPrincipalName?.Contains("#EXT#", StringComparison.OrdinalIgnoreCase) ?? true;
                    var isEmailVerified = !isGuestName && IsSameEmail(userPrincipalName, email);

                    MarkEmailVerified(context.Identity, isEmailVerified);

                    return Task.CompletedTask;
                };
            });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddTransient<INetptuneAuthService, NetptuneAuthService>();

        return services;
    }

    private static bool IsSameEmail(string? left, string? right)
    {
        return left is not null && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static void MarkEmailVerified(ClaimsIdentity? identity, bool isEmailVerified)
    {
        if (!isEmailVerified)
        {
            return;
        }

        identity?.AddClaim(new Claim(NetptuneClaims.ProviderEmailVerified, bool.TrueString));
    }

    public static IServiceCollection AddNetptuneApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationSchemes.ApiKey;
                options.DefaultChallengeScheme = AuthenticationSchemes.ApiKey;
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                AuthenticationSchemes.ApiKey,
                _ => { });

        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}
