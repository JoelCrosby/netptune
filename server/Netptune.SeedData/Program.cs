using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.SeedData;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetNetptuneConnectionString("netptune");

builder.Services.AddNetptuneEntities(options => options.ConnectionString = connectionString);
builder.Services.AddNetptuneSeedData();

using var host = builder.Build();

await host.StartAsync();
await host.StopAsync();
