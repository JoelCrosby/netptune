using System.Threading.RateLimiting;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

using Netptune.App.Endpoints;
using Netptune.App.Middleware;
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
builder.Services.AddSingleton<INotificationEventService, NotificationEventService>();

builder.Services.AddNetptuneIdentity().AddNetptuneIdentityEntities();
builder.Services.AddNeptuneAuthorization();
builder.Services.AddNeptuneAuthentication(options =>
{
    options.Issuer = configuration.GetRequiredValue("Tokens:Issuer");
    options.Audience = configuration.GetRequiredValue("Tokens:Audience");
    options.SecurityKey = configuration.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY");
    options.GitHubClientId = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_CLIENT_ID");
    options.GitHubSecret = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_SECRET");
    options.GitHubCallback = configuration.GetEnvironmentVariable("NETPTUNE_GITHUB_CALLBACK");
});

builder.AddNetptuneCache(options =>
{
    options.Connection = redisConnectionString;
});

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

builder.Services.AddCloudflareEmailService(options =>
{
    options.ApiToken = configuration.GetEnvironmentVariable("NETPTUNE_CLOUDFLARE_EMAIL_TOKEN");
    options.AccountId = configuration.GetEnvironmentVariable("NETPTUNE_CLOUDFLARE_ACCOUNT_ID");
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

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Transient;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddSlidingWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 300;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    options.AddSlidingWindowLimiter("import-export", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 2;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
});

builder.Services.AddValidation();

var app = builder.Build();

app.UseCorrelationId();
app.UseForwardedHeaders();
app.UseServerErrorLogging();

app.UseRouting();

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseWorkspaceValidation();

var apiGroup = app.MapGroup("/api")
    .RequireRateLimiting("api");

apiGroup.MapBoardEventsEndpoints();
apiGroup.MapNotificationsEndpoints();
apiGroup.MapActivityEndpoints();
apiGroup.MapAuditEndpoints();
apiGroup.MapAuthEndpoints();
apiGroup.MapBoardGroupsEndpoints();
apiGroup.MapBoardsEndpoints();
apiGroup.MapCommentsEndpoints();
apiGroup.MapMetaEndpoints();
apiGroup.MapProjectsEndpoints();
apiGroup.MapStorageEndpoints();
apiGroup.MapSprintsEndpoints();
apiGroup.MapTagsEndpoints();
apiGroup.MapTasksEndpoints();
apiGroup.MapUsersEndpoints();
apiGroup.MapWorkspacesEndpoints();
apiGroup.MapPublicEndpoints();

apiGroup.MapExportEndpoints()
    .RequireRateLimiting("import-export");
apiGroup.MapImportEndpoints()
    .RequireRateLimiting("import-export");

app.MapDefaultEndpoints();

app.Run();
