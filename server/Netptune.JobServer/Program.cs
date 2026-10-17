using Netptune.Automation;
using Netptune.Cache;
using Netptune.Core.Events;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.Export;
using Netptune.Import;
using Netptune.JobServer.Services;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Search;
using Netptune.ServiceDefaults;
using Netptune.ServiceDefaults.Middleware;
using Netptune.Services.Configuration;
using Netptune.Storage;
using Netptune.Transfer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetNetptuneConnectionString("netptune");
var redisConnectionString = builder.Configuration.GetNetptuneRedisConnectionString();

builder.AddNetptuneCache(options =>
{
    options.Connection = redisConnectionString;
});

builder.Services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneEntities(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneEventRecording();
builder.Services.AddNetptuneNotifications();
builder.Services.AddNetptuneBackgroundIdentity();

builder.Services.AddCloudflareEmailService();

builder.Services.AddS3StorageService();

builder.AddNetptuneSearch();

builder.Services.Configure<TransferOptions>(builder.Configuration.GetSection(TransferOptions.SectionName));
builder.Services.AddNetptuneExport();
builder.Services.AddNetptuneImport();

builder.Services.AddNetptuneAutomation(builder.Configuration);
builder.Services.AddHostedService<SearchSeedService>();
builder.Services.AddHostedService<EventOutboxPublisher>();
builder.Services.AddHostedService<ExportRetentionService>();
builder.Services.AddHostedService<AiWebDocumentRetentionService>();

builder.Services.AddWorkspaceFileReconciler();
builder.Services.AddHostedService<WorkspaceFileReconciliationService>();

builder.Services.AddNetptuneMessageQueue(
    builder.Configuration.GetNetptuneNatsConnectionString(),
    builder.Configuration,
    MessageKeys.Consumers.Jobs);

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Transient;
});

var app = builder.Build();

app.UseNetptuneRequestDefaults();

app.MapDefaultEndpoints();
app.Run();
