using Microsoft.AspNetCore.HttpOverrides;

using Netptune.App.Endpoints;
using Netptune.App.Services;
using Netptune.App.Utility;
using Netptune.Cache;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.ServiceDefaults;
using Netptune.Services.Authentication;
using Netptune.Services.Authorization;
using Netptune.Services.Configuration;
using Netptune.Storage;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var isDevelopment = builder.Environment.IsDevelopment();

builder.AddServiceDefaults();

var connectionString = configuration.GetNetptuneConnectionString("netptune");
var redisConnectionString = configuration.GetNetptuneRedisConnectionString();
var natsConnectionString = configuration.GetNetptuneNatsConnectionString();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(configuration.GetCorsOrigins())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");

            if (isDevelopment)
            {
                policy.DisallowCredentials();
                policy.AllowAnyOrigin();
            }
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton<BuildInfo>();
builder.Services.AddSingleton<IBoardEventService, BoardEventService>();

builder.Services.AddNetptuneIdentity().AddNetptuneIdentityEntities();
builder.Services.AddNeptuneAuthorization();
builder.Services.AddNeptuneAuthentication(options =>
{
    options.Issuer = configuration.GetRequiredValue("Tokens:Issuer");
    options.Audience = configuration.GetRequiredValue("Tokens:Audience");
    options.SecurityKey = configuration.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY");
    options.GitHubClientId = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_CLIENT_ID");
    options.GitHubSecret = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_SECRET");
});

builder.AddNetptuneCache(options =>
{
    options.Connection = redisConnectionString;
});

builder.Services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

builder.Services.AddNetptuneServices(options =>
{
    options.ClientOrigin = configuration.GetRequiredValue("Origin");
    options.ContentRootPath = builder.Environment.ContentRootPath;
});

builder.Services.AddSendGridEmailService(options =>
{
    options.SendGridApiKey = configuration.GetEnvironmentVariable("SEND_GRID_API_KEY");
    options.DefaultFromAddress = configuration.GetRequiredValue("Email:DefaultFromAddress");
    options.DefaultFromDisplayName = configuration.GetRequiredValue("Email:DefaultFromDisplayName");
});

builder.Services.AddS3StorageService(options =>
{
    options.BucketName = configuration.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
    options.Region = configuration.GetEnvironmentVariable("NETPTUNE_S3_REGION");
    options.AccessKeyID = configuration.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
    options.SecretAccessKey = configuration.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
});

builder.Services.AddNetptuneMessageQueue(natsConnectionString);

builder.Services.AddValidation();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

var apiGroup = app.MapGroup("/api");

apiGroup.MapBoardEventsEndpoints();
apiGroup.MapActivityEndpoints();
apiGroup.MapAuthEndpoints();
apiGroup.MapBoardGroupsEndpoints();
apiGroup.MapBoardsEndpoints();
apiGroup.MapCommentsEndpoints();
apiGroup.MapExportEndpoints();
apiGroup.MapImportEndpoints();
apiGroup.MapMetaEndpoints();
apiGroup.MapProjectsEndpoints();
apiGroup.MapStorageEndpoints();
apiGroup.MapTagsEndpoints();
apiGroup.MapTasksEndpoints();
apiGroup.MapUsersEndpoints();
apiGroup.MapWorkspacesEndpoints();
apiGroup.MapPublicEndpoints();

app.MapDefaultEndpoints();

app.Run();
