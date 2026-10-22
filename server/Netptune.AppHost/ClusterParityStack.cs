namespace Netptune.AppHost;

public sealed record NetptuneResources
{
    public required IResourceBuilder<ValkeyResource> Cache { get; init; }

    public required IResourceBuilder<PostgresServerResource> Postgres { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> PostgresDatabase { get; init; }

    public required IResourceBuilder<NatsServerResource> Nats { get; init; }

    public required IResourceBuilder<MeilisearchResource> Meilisearch { get; init; }

    public required IResourceBuilder<ProjectResource> Jobs { get; init; }

    public required IResourceBuilder<ProjectResource> Activity { get; init; }

    public required IResourceBuilder<ProjectResource> App { get; init; }

    public required IResourceBuilder<ProjectResource> Api { get; init; }
}

public static class ClusterParityStack
{
    extension(IDistributedApplicationBuilder builder)
    {
        public void AddClusterParity(NetptuneResources resources)
        {
            if (!ClusterParity.IsEnabled(builder))
            {
                return;
            }

            var edgeSecret = builder.AddParameter(
                "edge-authorization-secret",
                new GenerateParameterDefault { MinLength = 32, Special = false },
                secret: true,
                persist: true);

            ConfigureBackingServices(resources);
            ConfigureServers(resources, edgeSecret);

            builder.AddScheduledJobs(resources, edgeSecret);
            builder.AddEdge(resources, edgeSecret);
        }

        private void AddScheduledJobs(NetptuneResources resources, IResourceBuilder<ParameterResource> edgeSecret)
        {
            var postgres = resources.Postgres;

            // Like the CronJob, this archives expired audit rows to the bucket the Storage__S3__* settings name
            // before deleting them, so check which bucket that is before starting it.
            builder
                .AddProject<Projects.Netptune_ActivityServer>("activity-retention", launchProfileName: null)
                .WithArgs("--job", "retention")
                .WithCache(resources.Cache)
                .WithPostgres(resources.PostgresDatabase)
                .WithNats(resources.Nats)
                .WithProductionRuntime(512)
                .WithServerConfig(edgeSecret)
                .WithExplicitStart();

            // Dump and verify only. The chart's upload step would push a local database into whichever bucket
            // the Storage__S3__* settings name and prune backups there.
            builder
                .AddContainer("postgres-backup", "library/postgres", "18")
                .WithImageRegistry("docker.io")
                .WithEntrypoint("/bin/sh")
                .WithArgs(
                    "-c",
                    """
                    set -eu
                    pg_dump --dbname=netptune --format=custom --compress=6 --file=/backup/netptune.dump
                    pg_restore --list /backup/netptune.dump > /dev/null
                    echo "dump ok: $(stat -c %s /backup/netptune.dump) bytes"
                    """)
                .WithEnvironment("PGHOST", postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host))
                .WithEnvironment("PGPORT", postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port))
                .WithEnvironment("PGUSER", "postgres")
                .WithEnvironment("PGPASSWORD", postgres.Resource.PasswordParameter)
                .WithEnvironment("PGCONNECT_TIMEOUT", "30")
                .WithVolume("netptune-postgres-backup", "/backup")
                .WithMemoryLimit(512)
                .WaitFor(postgres)
                .WithExplicitStart();
        }

        private void AddEdge(NetptuneResources resources, IResourceBuilder<ParameterResource> edgeSecret)
        {
            var appEndpoint = resources.App.GetEndpoint("http");
            var apiEndpoint = resources.Api.GetEndpoint("http");
            var cspInlineScriptHash = ClusterParity.ReadCspInlineScriptHash(builder);

            var client = builder
                .AddDockerfile("client", "../../client")
                .WithBuildArg("NG_CONFIGURATION", "production,localhost")
                .WithHttpEndpoint(targetPort: 80)
                .WithEnvironment("API_URL", appEndpoint)
                .WithMemoryLimit(128);

            var website = builder
                .AddDockerfile("website", "../../website")
                .WithHttpEndpoint(targetPort: 80)
                .WithMemoryLimit(128);

            builder
                .AddContainer("edge", "library/traefik", "v3.6.13")
                .WithImageRegistry("docker.io")
                .WithBindMount("Edge", "/etc/traefik", isReadOnly: true)
                .WithHttpEndpoint(port: ClusterParity.EdgePort, targetPort: ClusterParity.EdgePort, isProxied: false)
                .WithEnvironment("ROOT_HOST", ClusterParity.RootHostName)
                .WithEnvironment("APP_HOST", ClusterParity.AppHostName)
                .WithEnvironment("API_HOST", ClusterParity.ApiHostName)
                .WithEnvironment("WEBSITE_HOST", ClusterParity.WebsiteHostName)
                .WithEnvironment("APP_ORIGIN", ClusterParity.AppOrigin)
                .WithEnvironment("APP_URL", appEndpoint)
                .WithEnvironment("API_URL", apiEndpoint)
                .WithEnvironment("CLIENT_URL", client.GetEndpoint("http"))
                .WithEnvironment("WEBSITE_URL", website.GetEndpoint("http"))
                .WithEnvironment("CSP_INLINE_SCRIPT_HASH", cspInlineScriptHash)
                .WithEnvironment("EDGE_AUTHORIZATION_HEADER", ClusterParity.EdgeAuthorizationHeader)
                .WithEnvironment("EDGE_AUTHORIZATION_SECRET", edgeSecret)
                .WithMemoryLimit(256)
                .WithExternalHttpEndpoints();
        }
    }

    private static void ConfigureBackingServices(NetptuneResources resources)
    {
        resources.Cache
            .WithDataVolume()
            .WithProductionMemoryPolicy("320mb")
            .WithMemoryLimit(512);

        resources.Postgres.WithMemoryLimit(1024);
        resources.Nats.WithMemoryLimit(512);

        resources.Meilisearch
            .WithEnvironment("MEILI_ENV", "production")
            .WithMemoryLimit(512);
    }

    private static void ConfigureServers(NetptuneResources resources, IResourceBuilder<ParameterResource> edgeSecret)
    {
        resources.Jobs.WithProductionRuntime(512).WithServerConfig(edgeSecret);
        resources.Activity.WithProductionRuntime(512).WithServerConfig(edgeSecret);

        resources.App
            .WithProductionRuntime(512)
            .WithServerConfig(edgeSecret)
            .WithTrustedProxies()
            .WithClientOrigin()
            .WithEnvironment("CorsOrigins__0", ClusterParity.AppOrigin)
            .WithEnvironment("Ai__MaxToolIterations", "40")
            .WithEnvironment("Ai__TurnTimeoutSeconds", "900")
            .WithEnvironment("Ai__MaxTurnCharacters", "400000");

        resources.Api
            .WithProductionRuntime(512)
            .WithServerConfig(edgeSecret)
            .WithTrustedProxies()
            .WithClientOrigin();
    }
}
