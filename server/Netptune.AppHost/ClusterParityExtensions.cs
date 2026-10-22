namespace Netptune.AppHost;

public static class ClusterParityExtensions
{
    private const long Mebibyte = 1024 * 1024;

    extension(IDistributedApplicationBuilder builder)
    {
        // The launch profiles pin Development and the dev ports, so cluster-parity services skip them and
        // declare their own endpoint instead.
        public IResourceBuilder<ProjectResource> AddNetptuneProject<TProject>(string name, int? clusterPort = null)
            where TProject : IProjectMetadata, new()
        {
            var clusterParity = ClusterParity.IsEnabled(builder);

            if (!clusterParity)
            {
                return builder.AddProject<TProject>(name);
            }

            var project = builder.AddProject<TProject>(name, launchProfileName: null);

            if (clusterPort is { } port)
            {
                return project.WithHttpEndpoint(port: port, isProxied: false);
            }

            return project.WithHttpEndpoint();
        }

        public IResourceBuilder<ProjectResource>? AddSeedData(IResourceBuilder<PostgresDatabaseResource> database)
        {
            var clusterParity = ClusterParity.IsEnabled(builder);

            if (clusterParity)
            {
                return null;
            }

            return builder
                .AddProject<Projects.Netptune_SeedData>("seed-data")
                .WithPostgres(database)
                .WithParentRelationship(database);
        }
    }

    extension<T>(IResourceBuilder<T> builder) where T : IResourceWithWaitSupport
    {
        public IResourceBuilder<T> WaitForSeedData(IResourceBuilder<ProjectResource>? seedData)
        {
            if (seedData is null)
            {
                return builder;
            }

            return builder.WaitForCompletion(seedData);
        }
    }

    extension(IResourceBuilder<ProjectResource> builder)
    {
        // A pod's memory limit caps the GC heap at 75% of it. A host process has no cgroup for the runtime
        // to read, so the cap is set explicitly to make memory regressions fail the same way.
        public IResourceBuilder<ProjectResource> WithProductionRuntime(int memoryLimitMebibytes)
        {
            var heapHardLimit = memoryLimitMebibytes * Mebibyte * 3 / 4;
            var heapHardLimitHex = $"0x{heapHardLimit:X}";

            return builder
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
                .WithEnvironment("DOTNET_ENVIRONMENT", "Production")
                .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
                .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory")
                .WithEnvironment("DOTNET_GCHeapHardLimit", heapHardLimitHex);
        }

        public IResourceBuilder<ProjectResource> WithServerConfig(IResourceBuilder<ParameterResource> edgeSecret)
        {
            return builder
                .WithEnvironment("TrustedProxies__EdgeAuthorizationHeader", ClusterParity.EdgeAuthorizationHeader)
                .WithEnvironment("TrustedProxies__EdgeAuthorizationSecret", edgeSecret)
                .WithEnvironment("Turnstile__SecretKey", ClusterParity.TurnstileTestSecretKey);
        }

        public IResourceBuilder<ProjectResource> WithTrustedProxies()
        {
            return builder.WithEnvironment(context =>
            {
                context.EnvironmentVariables["TrustedProxies__ClientAddressHeader"] = ClusterParity.ClientAddressHeader;

                for (var index = 0; index < ClusterParity.TrustedNetworks.Length; index++)
                {
                    var network = ClusterParity.TrustedNetworks[index];

                    context.EnvironmentVariables[$"TrustedProxies__Networks__{index}"] = network;
                }
            });
        }

        public IResourceBuilder<ProjectResource> WithClientOrigin()
        {
            return builder.WithEnvironment("Origin", $"{ClusterParity.AppOrigin}/");
        }
    }

    extension<T>(IResourceBuilder<T> builder) where T : ContainerResource
    {
        // Runtime args are left out of the hash Aspire uses to decide whether a persistent container needs
        // recreating, so the limit is mirrored into the environment, which is part of it.
        public IResourceBuilder<T> WithMemoryLimit(int memoryLimitMebibytes)
        {
            var memoryLimit = $"{memoryLimitMebibytes}m";

            return builder
                .WithContainerRuntimeArgs($"--memory={memoryLimit}")
                .WithEnvironment("NETPTUNE_MEMORY_LIMIT", memoryLimit);
        }
    }

    extension(IResourceBuilder<ValkeyResource> builder)
    {
        // Valkey's arguments are a single `sh -c` script, so the flags cannot be appended and the whole
        // command is replaced. volatile-lru spares the DataProtection key ring, which is stored without a TTL.
        public IResourceBuilder<ValkeyResource> WithProductionMemoryPolicy(string maxMemory)
        {
            return builder.WithArgs(context =>
            {
                context.Args.Clear();
                context.Args.Add("-c");
                context.Args.Add(
                    "valkey-server --requirepass $VALKEY_PASSWORD --dir /data --appendonly yes " +
                    $"--maxmemory {maxMemory} --maxmemory-policy volatile-lru");
            });
        }
    }
}
