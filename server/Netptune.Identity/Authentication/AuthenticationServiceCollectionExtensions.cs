using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Services;

namespace Netptune.Identity.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    private const string ExternalAuthLoggerCategory = "Netptune.Identity.Authentication.ExternalAuth";

    public static IdentityBuilder AddNetptuneIdentity(this IServiceCollection services)
    {
        return services.AddIdentity<AppUser, IdentityRole>()
            .AddDefaultTokenProviders();
    }

    public static void AddNeptuneAuthentication(this IServiceCollection services, Action<NetptuneAuthenticationOptions> action)
    {
        var authenticationOptions = ConfigureServices(services, action);

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
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })

        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authenticationOptions.Issuer,
                ValidAudience = authenticationOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationOptions.SecurityKey)),
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
            options.ClientId = authenticationOptions.GitHubClientId;
            options.ClientSecret = authenticationOptions.GitHubSecret;
            options.CallbackPath = authenticationOptions.GitHubCallback;
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

                LogProviderRedirect(context.HttpContext, AuthenticationSchemes.Github, options.CallbackPath, options.SignInScheme, original, redirect);

                context.Response.Redirect(redirect);

                return Task.CompletedTask;
            };
            options.Events.OnCreatingTicket = async context =>
            {
                LogCreatingTicketStarted(context.HttpContext, AuthenticationSchemes.Github, context.Identity);

                var token = context.AccessToken;
                var factory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient();

                client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
                client.DefaultRequestHeaders.Add("User-Agent", "Netptune API");

                GithubUserResponse? user;

                try
                {
                    user = await client.GetFromJsonAsync<GithubUserResponse>("https://api.github.com/user");
                }
                catch (Exception exception)
                {
                    GetExternalAuthLogger(context.HttpContext).LogWarning(
                        exception,
                        "GitHub user profile request failed during external auth ticket creation. Provider: {Provider}; HasAccessToken: {HasAccessToken}",
                        AuthenticationSchemes.Github,
                        !string.IsNullOrWhiteSpace(token));

                    throw;
                }

                if (user is null)
                {
                    GetExternalAuthLogger(context.HttpContext).LogWarning(
                        "GitHub user profile request returned no user during external auth ticket creation. Provider: {Provider}; HasAccessToken: {HasAccessToken}",
                        AuthenticationSchemes.Github,
                        !string.IsNullOrWhiteSpace(token));

                    return;
                }

                context.Identity?.AddClaim(new Claim("Provider-Picture-Url", user.AvatarUrl.ToString()));

                LogCreatingTicketCompleted(context.HttpContext, AuthenticationSchemes.Github, context.Identity);
            };
            options.Events.OnTicketReceived = context =>
            {
                LogTicketReceived(
                    context.HttpContext,
                    AuthenticationSchemes.Github,
                    options.SignInScheme,
                    context.ReturnUri,
                    context.Principal,
                    context.Properties?.Items.Keys);

                return Task.CompletedTask;
            };
            options.Events.OnRemoteFailure = context =>
            {
                LogRemoteFailure(context.HttpContext, AuthenticationSchemes.Github, context.Failure, context.Properties?.RedirectUri);

                return Task.CompletedTask;
            };
        });

        services.AddAuthentication()
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authenticationOptions.GoogleClientId;
                options.ClientSecret = authenticationOptions.GoogleSecret;
                options.CallbackPath = authenticationOptions.GoogleCallback;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = true;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    LogProviderRedirect(
                        context.HttpContext,
                        AuthenticationSchemes.Google,
                        options.CallbackPath,
                        options.SignInScheme,
                        context.RedirectUri,
                        context.RedirectUri);

                    context.Response.Redirect(context.RedirectUri);

                    return Task.CompletedTask;
                };
                options.Events.OnCreatingTicket = context =>
                {
                    LogCreatingTicketStarted(context.HttpContext, AuthenticationSchemes.Google, context.Identity);

                    var pictureUrl = context.User.GetProperty("picture").GetString();

                    if (pictureUrl is not null)
                    {
                        context.Identity?.AddClaim(new Claim("Provider-Picture-Url", pictureUrl));
                    }

                    LogCreatingTicketCompleted(context.HttpContext, AuthenticationSchemes.Google, context.Identity);

                    return Task.CompletedTask;
                };
                options.Events.OnTicketReceived = context =>
                {
                    LogTicketReceived(
                        context.HttpContext,
                        AuthenticationSchemes.Google,
                        options.SignInScheme,
                        context.ReturnUri,
                        context.Principal,
                        context.Properties?.Items.Keys);

                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    LogRemoteFailure(context.HttpContext, AuthenticationSchemes.Google, context.Failure, context.Properties?.RedirectUri);

                    return Task.CompletedTask;
                };
            })
            .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authenticationOptions.MicrosoftClientId;
                options.ClientSecret = authenticationOptions.MicrosoftSecret;
                options.CallbackPath = authenticationOptions.MicrosoftCallback;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = true;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    LogProviderRedirect(
                        context.HttpContext,
                        AuthenticationSchemes.Microsoft,
                        options.CallbackPath,
                        options.SignInScheme,
                        context.RedirectUri,
                        context.RedirectUri);

                    context.Response.Redirect(context.RedirectUri);

                    return Task.CompletedTask;
                };
                options.Events.OnCreatingTicket = context =>
                {
                    LogCreatingTicketStarted(context.HttpContext, AuthenticationSchemes.Microsoft, context.Identity);
                    LogCreatingTicketCompleted(context.HttpContext, AuthenticationSchemes.Microsoft, context.Identity);

                    return Task.CompletedTask;
                };
                options.Events.OnTicketReceived = context =>
                {
                    LogTicketReceived(
                        context.HttpContext,
                        AuthenticationSchemes.Microsoft,
                        options.SignInScheme,
                        context.ReturnUri,
                        context.Principal,
                        context.Properties?.Items.Keys);

                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    LogRemoteFailure(context.HttpContext, AuthenticationSchemes.Microsoft, context.Failure, context.Properties?.RedirectUri);

                    return Task.CompletedTask;
                };
            });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddTransient<INetptuneAuthService, NetptuneAuthService>();
    }

    private static NetptuneAuthenticationOptions ConfigureServices(
        IServiceCollection services, Action<NetptuneAuthenticationOptions> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        var options = new NetptuneAuthenticationOptions();

        action(options);

        services.Configure(action);

        return options;
    }

    private static void LogProviderRedirect(
        HttpContext context,
        string provider,
        PathString callbackPath,
        string signInScheme,
        string originalRedirectUri,
        string outgoingRedirectUri)
    {
        GetExternalAuthLogger(context).LogInformation(
            "External auth redirecting to provider. Provider: {Provider}; RequestPath: {RequestPath}; CallbackPath: {CallbackPath}; SignInScheme: {SignInScheme}; ProviderHost: {ProviderHost}; ProviderRedirectPath: {ProviderRedirectPath}; ProviderRedirectUriParameter: {ProviderRedirectUriParameter}; RedirectWasRewritten: {RedirectWasRewritten}; CookieNames: {CookieNames}",
            provider,
            context.Request.Path.Value,
            callbackPath.Value,
            signInScheme,
            TryGetUriPart(outgoingRedirectUri, uri => uri.Host),
            TryGetUriPart(outgoingRedirectUri, uri => uri.AbsolutePath),
            TryGetRedirectUriParameter(outgoingRedirectUri),
            !string.Equals(originalRedirectUri, outgoingRedirectUri, StringComparison.Ordinal),
            Join(context.Request.Cookies.Keys));
    }

    private static void LogCreatingTicketStarted(HttpContext context, string provider, ClaimsIdentity? identity)
    {
        GetExternalAuthLogger(context).LogInformation(
            "External auth ticket creation started. Provider: {Provider}; CallbackPath: {CallbackPath}; QueryKeys: {QueryKeys}; IdentityAuthenticated: {IdentityAuthenticated}; ClaimTypes: {ClaimTypes}",
            provider,
            context.Request.Path.Value,
            Join(context.Request.Query.Keys),
            identity?.IsAuthenticated,
            Join(identity?.Claims.Select(claim => claim.Type)));
    }

    private static void LogCreatingTicketCompleted(HttpContext context, string provider, ClaimsIdentity? identity)
    {
        GetExternalAuthLogger(context).LogInformation(
            "External auth ticket creation completed. Provider: {Provider}; CallbackPath: {CallbackPath}; IdentityAuthenticated: {IdentityAuthenticated}; ClaimTypes: {ClaimTypes}",
            provider,
            context.Request.Path.Value,
            identity?.IsAuthenticated,
            Join(identity?.Claims.Select(claim => claim.Type)));
    }

    private static void LogTicketReceived(
        HttpContext context,
        string provider,
        string signInScheme,
        string? returnUri,
        ClaimsPrincipal? principal,
        IEnumerable<string>? propertyKeys)
    {
        GetExternalAuthLogger(context).LogInformation(
            "External auth ticket received from provider. Provider: {Provider}; CallbackPath: {CallbackPath}; SignInScheme: {SignInScheme}; ReturnUri: {ReturnUri}; PrincipalIdentities: {PrincipalIdentities}; ClaimTypes: {ClaimTypes}; PropertyKeys: {PropertyKeys}",
            provider,
            context.Request.Path.Value,
            signInScheme,
            returnUri,
            Join(principal?.Identities.Select(identity => $"{identity.AuthenticationType}:{identity.IsAuthenticated}")),
            Join(principal?.Claims.Select(claim => claim.Type).Distinct()),
            Join(propertyKeys));
    }

    private static void LogRemoteFailure(HttpContext context, string provider, Exception? failure, string? redirectUri)
    {
        GetExternalAuthLogger(context).LogWarning(
            failure,
            "External auth remote failure. Provider: {Provider}; CallbackPath: {CallbackPath}; QueryKeys: {QueryKeys}; Error: {Error}; ErrorDescription: {ErrorDescription}; RedirectUri: {RedirectUri}; CookieNames: {CookieNames}",
            provider,
            context.Request.Path.Value,
            Join(context.Request.Query.Keys),
            Truncate(context.Request.Query["error"].ToString(), 200),
            Truncate(context.Request.Query["error_description"].ToString(), 500),
            redirectUri,
            Join(context.Request.Cookies.Keys));
    }

    private static ILogger GetExternalAuthLogger(HttpContext context)
    {
        return context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(ExternalAuthLoggerCategory);
    }

    private static string TryGetRedirectUriParameter(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return "<invalid>";
        }

        var query = QueryHelpers.ParseQuery(parsed.Query);

        return query.TryGetValue("redirect_uri", out var redirectUri)
            ? redirectUri.ToString()
            : "<none>";
    }

    private static string TryGetUriPart(string uri, Func<Uri, string> getPart)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
            ? getPart(parsed)
            : "<invalid>";
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "<none>";

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string Join(IEnumerable<string>? values)
    {
        if (values is null) return "<none>";

        var joined = string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(joined) ? "<none>" : joined;
    }
}
