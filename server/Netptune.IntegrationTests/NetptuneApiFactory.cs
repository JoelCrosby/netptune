using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.App;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.ConnectionFactories;
using Netptune.Services.Cache.Redis;

using Xunit;

namespace Netptune.IntegrationTests;

public class NetptuneApiFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer DbContainer =
        new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithImage("postgres:latest")
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "netptune",
                Username = "netptune",
                Password = "netptune",
            })
            .Build();

    private readonly RedisTestcontainer CacheContainer =
        new TestcontainersBuilder<RedisTestcontainer>()
            .WithImage("redis:latest")
            .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(RedisCacheOptions));
            services.Configure<RedisCacheOptions>(options => options.Connection = CacheContainer.Hostname);

            services.RemoveAll(typeof(IDbConnectionFactory));
            services.AddScoped<IDbConnectionFactory>(_ => new NetptuneConnectionFactory(DbContainer.ConnectionString));

            services.RemoveAll(typeof(DbContext));
            services.AddDbContext<DataContext>(options =>
            {
                options
                    .UseNpgsql(DbContainer.ConnectionString)
                    .UseSnakeCaseNamingConvention();
            });
        });
    }

    public async Task InitializeAsync()
    {
        await CacheContainer.StartAsync();
        await DbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await CacheContainer.StopAsync();
        await DbContainer.StopAsync();
    }
}
