using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.App;
using Netptune.Core.Authorization;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.ConnectionFactories;
using Netptune.Services.Authorization.Requirements;
using Netptune.Services.Cache.Redis;

using Xunit;

namespace Netptune.IntegrationTests;

internal class ContainerConnection
{
    public string DdConnection { get; set; } = null!;

    public string RedisConnection { get; set; } = null!;
}

public sealed class NetptuneApiFactory : WebApplicationFactory<Startup>, IAsyncLifetime
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

            // services.RemoveAll(typeof(DbContext));
            services.RemoveAll(typeof(DataContext));
            services.RemoveAll(typeof(DbContextOptions<DataContext>));
            services.AddDbContext<DataContext>(options =>
            {
                options
                    .UseNpgsql(DbContainer.ConnectionString)
                    .UseSnakeCaseNamingConvention();
            });

            services.AddSingleton(new ContainerConnection
            {
                RedisConnection = CacheContainer.Hostname,
                DdConnection = DbContainer.ConnectionString,
            });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(TestAuthenticationHandler.AuthenticationScheme)
                    .Build();

                options.AddPolicy(NetptunePolicies.Workspace, config => config.RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(TestAuthenticationHandler.AuthenticationScheme)
                    .AddRequirements(new WorkspaceRequirement())
                    .Build());
            });

            services
                .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme, _ => { });

            services.AddHostedService<DataSeedService>();
        });
    }

    public async Task InitializeAsync()
    {
        await CacheContainer.StartAsync();
        await DbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await CacheContainer.DisposeAsync();
        await DbContainer.DisposeAsync();
    }

    public HttpClient CreateNetptuneClient()
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization = new ("TestScheme");
        client.DefaultRequestHeaders.Add("workspace", "netptune");

        return client;
    }
}
