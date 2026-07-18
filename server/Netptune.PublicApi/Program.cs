using Microsoft.AspNetCore.HttpOverrides;

using Netptune.Cache;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.Handlers;
using Netptune.Identity.Authentication;
using Netptune.Identity.Authorization;
using Netptune.PublicApi.Configuration;
using Netptune.PublicApi.Endpoints;
using Netptune.PublicApi.Middleware;
using Netptune.Repositories.Configuration;
using Netptune.ServiceDefaults;
using Netptune.Services.Configuration;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Mediator generates registrations for the entire shared Netptune.Handlers assembly.
// This host maps only its public subset, so app-only handler dependencies must not
// prevent the independently hosted API from starting in Development.
builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);

builder.AddServiceDefaults();

var connectionString = configuration.GetNetptuneConnectionString("netptune");
var redisConnectionString = configuration.GetNetptuneRedisConnectionString();
var natsConnectionString = configuration.GetNetptuneNatsConnectionString();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddNetptuneIdentity().AddNetptuneIdentityEntities();
builder.Services.AddNeptuneAuthorization(AuthenticationSchemes.ApiKey);
builder.Services.AddNeptuneApiKeyAuthentication();

builder.AddNetptuneCache(options => options.Connection = redisConnectionString);

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);

builder.Services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneEntities(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneServices(options =>
{
    options.ClientOrigin = configuration.GetRequiredValue("Origin");
    options.ContentRootPath = builder.Environment.ContentRootPath;
});

builder.Services.AddNetptuneMessageQueue(natsConnectionString);
builder.Services.AddNetptuneHandlers();
builder.Services.AddPublicApiRateLimiter();
builder.Services.AddValidation();
builder.Services.AddPublicApiOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<PreAuthenticationRateLimiterMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGroup("/api/v1")
    .RequireRateLimiting(PublicApiRateLimiter.PolicyName)
    .MapPublicApiV1Endpoints();

app.MapDefaultEndpoints();
app.MapOpenApi();

app.MapScalarApiReference("/docs", options => options
    .WithTitle("Netptune Public API")
    .AddPreferredSecuritySchemes(PublicApiOpenApi.SecuritySchemeName)
    .DisableDefaultFonts()
    .DisableAgent())
    .AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/docs"))
    .ExcludeFromDescription()
    .AllowAnonymous();

app.Run();
