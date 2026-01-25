using System.Net;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.IntegrationTests.TestServices;
using Netptune.Services.Authorization.Requirements;

using Testcontainers.PostgreSql;
using Testcontainers.Redis;

using Xunit;

namespace Netptune.IntegrationTests;

internal sealed class Collections
{
    public const string Database = "Database collection";
}

[CollectionDefinition(Collections.Database)]
public sealed class DatabaseCollection : ICollectionFixture<NetptuneApiFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public sealed class NetptuneApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:15.1").Build();
    private readonly RedisContainer CacheContainer = new RedisBuilder("redis:7.0").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        LoadEnvironmentVariables();

        Environment.SetEnvironmentVariable("DATABASE_URL", DbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("REDIS_URL", CacheContainer.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.Replace<IStorageService, TestStorageService>();

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
        var client = CreateDefaultClient(new TestExceptionHttpHandler());

        client.DefaultRequestHeaders.Authorization = new ("TestScheme");
        client.DefaultRequestHeaders.Add("workspace", "netptune");

        return client;
    }
}

internal class TestExceptionHttpHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage>
        SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.InternalServerError)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            InternalServerErrorException.Throw(content);
        }

        return response;
    }
}

internal class InternalServerErrorException : Exception
{
    private InternalServerErrorException(string message) : base (message)
    {
    }

    public static void Throw(string message) => throw new InternalServerErrorException(message);
}

internal static class ServiceCollectionExtensions
{
    public static void Replace<TService, TReplacement>(this IServiceCollection services)
        where TService : class
        where TReplacement : class, TService
    {
        services.RemoveAll(typeof(TService));
        services.AddSingleton<TService, TReplacement>();
    }
}
