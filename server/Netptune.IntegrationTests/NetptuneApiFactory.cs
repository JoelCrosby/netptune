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
using Netptune.Core.Cache.Common;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.ConnectionFactories;
using Netptune.Services.Authorization.Requirements;
using Netptune.Services.Cache.Redis;

using Testcontainers.PostgreSql;
using Testcontainers.Redis;

using Xunit;

namespace Netptune.IntegrationTests;

internal class ContainerConnection
{
    public required string DdConnection { get; init; }

    public required string RedisConnection { get; init; }
}

public sealed class NetptuneApiFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .Build();

    private readonly RedisContainer CacheContainer = new RedisBuilder()
        .WithPortBinding(6379, true)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        LoadEnvironmentVariables();

        var postgresConnection = DbContainer.GetConnectionString();
        var redisConnection = CacheContainer.GetConnectionString();

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(ICacheProvider));
            services.AddNetptuneRedis(options => options.Connection = redisConnection);

            services.RemoveAll(typeof(IDbConnectionFactory));
            services.AddScoped<IDbConnectionFactory>(_ => new NetptuneConnectionFactory(postgresConnection));

            services.RemoveAll(typeof(DataContext));
            services.RemoveAll(typeof(DbContextOptions<DataContext>));
            services.AddDbContext<DataContext>(options =>
            {
                options
                    .UseNpgsql(postgresConnection)
                    .UseSnakeCaseNamingConvention();
            });

            services.AddSingleton(new ContainerConnection
            {
                DdConnection = postgresConnection,
                RedisConnection = redisConnection,
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

    private static void LoadEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("NETPTUNE_SIGNING_KEY", "test");
        Environment.SetEnvironmentVariable("NETPTUNE_GITHUB_CLIENT_ID", "test");
        Environment.SetEnvironmentVariable("NETPTUNE_GITHUB_SECRET", "test");
        Environment.SetEnvironmentVariable("SEND_GRID_API_KEY", "test");
        Environment.SetEnvironmentVariable("NETPTUNE_S3_BUCKET_NAME", "test");
        Environment.SetEnvironmentVariable("NETPTUNE_S3_REGION", "test");
        Environment.SetEnvironmentVariable("NETPTUNE_S3_ACCESS_KEY_ID", "test");
        Environment.SetEnvironmentVariable("NETPTUNE_S3_SECRET_ACCESS_KEY", "test");
    }

    public async Task InitializeAsync()
    {
        await CacheContainer.StartAsync().ConfigureAwait(false);
        await DbContainer.StartAsync().ConfigureAwait(false);
    }

    public new async Task DisposeAsync()
    {
        await CacheContainer.DisposeAsync().ConfigureAwait(false);
        await DbContainer.DisposeAsync().ConfigureAwait(false);
    }

    public HttpClient CreateNetptuneClient()
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization = new ("TestScheme");
        client.DefaultRequestHeaders.Add("workspace", "netptune");

        return client;
    }
}
