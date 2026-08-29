using Microsoft.AspNetCore.HttpOverrides;

using Netptune.Api.Configuration;
using Netptune.Api.Endpoints;
using Netptune.Api.Middleware;
using Netptune.Automation.Actions;
using Netptune.Cache;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.Handlers;
using Netptune.Identity.Authentication;
using Netptune.Identity.Authorization;
using Netptune.Query;
using Netptune.Repositories.Configuration;
using Netptune.Search;
using Netptune.ServiceDefaults;
using Netptune.ServiceDefaults.Middleware;
using Netptune.Services.Configuration;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Mediator registers every handler in the shared Netptune.Handlers assembly, including the
// app-only ones whose dependencies this slim host never wires up. ApiContainerTests
// pins exactly which services those are, so the gap cannot widen unnoticed.
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
builder.Services.AddNetptuneAuthorization(AuthenticationSchemes.ApiKey);
builder.Services.AddNetptuneApiKeyAuthentication();

builder.AddNetptuneCache(options => options.Connection = redisConnectionString);
builder.AddNetptuneSearch();

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
builder.Services.AddNetptuneQuery();
builder.Services.AddNetptuneAutomationActions();
builder.Services.AddApiRateLimiter();
builder.Services.AddValidation();
builder.Services.AddApiOpenApi();

var app = builder.Build();

app.UseNetptuneRequestDefaults();
app.UseForwardedHeaders();
app.UseMiddleware<ContentSecurityPolicyMiddleware>();
app.UseMiddleware<PreAuthenticationRateLimiterMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGroup("/api/v1")
    .RequireRateLimiting(ApiRateLimiter.PolicyName)
    .MapApiV1Endpoints();

app.MapDefaultEndpoints();
app.MapOpenApi().AllowAnonymous();

app.MapScalarApiReference("/docs", options => options
    .WithTitle("Netptune API")
    .AddPreferredSecuritySchemes(ApiOpenApi.SecuritySchemeName)
    .WithNonce()
    .DisableDefaultFonts()
    .DisableAgent())
    .AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/docs"))
    .ExcludeFromDescription()
    .AllowAnonymous();

app.Run();
