using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Events;
using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.Events;
using Netptune.JobServer.Services;
using Netptune.Messaging;
using Netptune.Repositories.Configuration;
using Netptune.Services.Cache.Redis;
using Netptune.Services.Configuration;
using Netptune.Storage;

var builder = WebApplication.CreateBuilder();

var connectionString = builder.Configuration.GetNetptuneConnectionString("netptune");
var redisConnectionString = builder.Configuration.GetNetptuneRedisConnectionString();
var rabbitMqConnectionString = builder.Configuration.GetNetptuneRabbitMqConnectionString();

builder.Services.AddNetptuneRedis(options =>
{
    options.Connection = redisConnectionString;
});

builder.Services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneEntities(options => options.ConnectionString = connectionString);

builder.Services.AddNetptuneServices(options =>
{
    options.ClientOrigin = builder.Configuration.GetRequiredValue("Origin");
    options.ContentRootPath = builder.Environment.ContentRootPath;
});

builder.Services.AddSendGridEmailService(options =>
{
    options.SendGridApiKey = builder.Configuration.GetEnvironmentVariable("SEND_GRID_API_KEY");
    options.DefaultFromAddress = builder.Configuration.GetRequiredValue("Email:DefaultFromAddress");
    options.DefaultFromDisplayName = builder.Configuration.GetRequiredValue("Email:DefaultFromDisplayName");
});

builder.Services.AddS3StorageService(options =>
{
    options.BucketName = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME");
    options.Region = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_REGION");
    options.AccessKeyID = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID");
    options.SecretAccessKey = builder.Configuration.GetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY");
});

builder.Services.AddActivitySink();
builder.Services.AddHostedService<QueueConsumerService>();

builder.Services.AddNetptuneEvents(options =>
{
    options.ConnectionString = rabbitMqConnectionString;
});

builder.Services.AddMediatR(options =>
{
    options.Lifetime = ServiceLifetime.Transient;
    options.RegisterServicesFromAssemblyContaining(typeof(Program));
});

var app = builder.Build();

app.Run();
