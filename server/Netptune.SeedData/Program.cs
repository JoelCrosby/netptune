using Netptune.Core.Extensions;
using Netptune.Entities.Configuration;
using Netptune.SeedData;
using Netptune.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetNetptuneConnectionString("netptune");

builder.Services.AddNetptuneEntities(options =>
{
    options.ConnectionString = connectionString;
    options.EnsureCreatedOnStartup = false;
});

builder.Services.AddNetptuneSeedData();

using var host = builder.Build();

await host.StartAsync();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var seeder = host.Services.GetRequiredService<SeedDataRunner>();

await seeder.RunAsync(lifetime.ApplicationStopping);
await host.StopAsync();
