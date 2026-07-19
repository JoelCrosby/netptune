using Netptune.Activity;
using Netptune.Cache;
using Netptune.Core.Events;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.Repositories.Configuration;
using Netptune.ServiceDefaults;
using Netptune.Storage;

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

builder.Services.AddS3StorageService(options =>
{
    options.BucketName = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
    options.Region = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_REGION");
    options.AccessKeyID = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
    options.SecretAccessKey = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
});

builder.Services.AddNetptuneActivity(builder.Configuration);

builder.Services.AddNetptuneMessageQueue(
    builder.Configuration.GetNetptuneNatsConnectionString(),
    builder.Configuration,
    MessageKeys.Consumers.Activity);
builder.Services.AddCanonicalEventConsumer();

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Transient;
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.Run();
