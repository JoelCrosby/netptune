using System;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.App.Endpoints;
using Netptune.App.Hubs;
using Netptune.App.Utility;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Authentication;
using Netptune.Services.Authorization;
using Netptune.Services.Cache.Redis;
using Netptune.Services.Configuration;
using Netptune.Storage;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

var connectionString = configuration.GetNetptuneConnectionString("netptune");
var redisConnectionString = configuration.GetNetptuneRedisConnectionString();
var zeroMqConnectionString = configuration.GetNetptuneZeroMqConnectionString();

services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(configuration.GetCorsOrigins())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("Content-Disposition")
    );
});

services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

services.AddSingleton<BuildInfo>();

services.AddSignalR(options =>
{
    options.EnableDetailedErrors = environment.IsDevelopment();
});

services.AddNetptuneIdentity().AddNetptuneIdentityEntities();
services.AddNeptuneAuthorization();
services.AddNeptuneAuthentication(options =>
{
    options.Issuer = configuration.GetRequiredValue("Tokens:Issuer");
    options.Audience = configuration.GetRequiredValue("Tokens:Audience");
    options.SecurityKey = configuration.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY");
    options.GitHubClientId = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_CLIENT_ID");
    options.GitHubSecret = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_SECRET");
});

services.AddNetptuneRedis(options =>
{
    options.Connection = redisConnectionString;
});

services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

services.AddNetptuneServices(options =>
{
    options.ClientOrigin = configuration.GetRequiredValue("Origin");
    options.ContentRootPath = environment.ContentRootPath;
});

services.AddSendGridEmailService(options =>
{
    options.SendGridApiKey = configuration.GetEnvironmentVariable("SEND_GRID_API_KEY");
    options.DefaultFromAddress = configuration.GetRequiredValue("Email:DefaultFromAddress");
    options.DefaultFromDisplayName = configuration.GetRequiredValue("Email:DefaultFromDisplayName");
});

services.AddS3StorageService(options =>
{
    options.BucketName = configuration.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
    options.Region = configuration.GetEnvironmentVariable("NETPTUNE_S3_REGION");
    options.AccessKeyID = configuration.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
    options.SecretAccessKey = configuration.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
});

services.AddSpaStaticFiles(options =>
{
    options.RootPath = Path.Join(environment.WebRootPath, "dist");
});

services.AddNetptuneMessageQueue(options =>
{
    options.ConnectionString = zeroMqConnectionString;
});

services.AddValidation();

var app = builder.Build();

app.UseForwardedHeaders();

if (environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";
        return next(context);
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSpaStaticFiles();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<BoardHub>(BoardHub.Path);

var api = app.MapGroup("/api");

ActivityEndpoints.Map(api);
AuthEndpoints.Map(api);
BoardGroupsEndpoints.Map(api);
BoardsEndpoints.Map(api);
CommentsEndpoints.Map(api);
ExportEndpoints.Map(api);
ImportEndpoints.Map(api);
MetaEndpoints.Map(api);
ProjectsEndpoints.Map(api);
StorageEndpoints.Map(api);
TagsEndpoints.Map(api);
TasksEndpoints.Map(api);
UsersEndpoints.Map(api);
WorkspacesEndpoints.Map(api);

app.UseSpa(_ => { });

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
