namespace Netptune.AppHost;

public static class ResourceBuilderExtensions
{
    extension<T>(IResourceBuilder<T> builder) where T : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        public IResourceBuilder<T> WithCache(IResourceBuilder<ValkeyResource> cache)
        {
            return builder
                .WaitFor(cache)
                .WithReference(cache)
                .WithEnvironment("REDIS_URL", cache.Resource.ConnectionStringExpression);
        }

        public IResourceBuilder<T> WithPostgres(IResourceBuilder<PostgresDatabaseResource> database)
        {
            return builder
                .WaitFor(database)
                .WithReference(database)
                .WithEnvironment("ConnectionStrings__netptune", database.Resource.ConnectionStringExpression);
        }

        public IResourceBuilder<T> WithNats(IResourceBuilder<NatsServerResource> nats)
        {
            return builder
                .WithReference(nats);
        }

        public IResourceBuilder<T> WithJobServer(IResourceBuilder<ProjectResource> jobs)
        {
            return builder
                .WaitFor(jobs)
                .WithReference(jobs);
        }
    }

    extension(IResourceBuilder<DbGateContainerResource> builder)
    {
        public IResourceBuilder<DbGateContainerResource> WithValkey(IResourceBuilder<ValkeyResource> valkey)
        {
            var resource = valkey.Resource;
            var id = DbGateBuilderExtensions.SanitizeConnectionId(resource.Name);

            return builder
                .WaitFor(valkey)
                .WithEnvironment(context =>
                {
                    context.EnvironmentVariables[$"LABEL_{id}"] = resource.Name;
                    context.EnvironmentVariables[$"ENGINE_{id}"] = "redis@dbgate-plugin-redis";
                    context.EnvironmentVariables[$"SERVER_{id}"] = resource.Name;
                    context.EnvironmentVariables[$"PORT_{id}"] = resource.PrimaryEndpoint.Property(EndpointProperty.TargetPort);

                    if (resource.PasswordParameter is { } password)
                    {
                        context.EnvironmentVariables[$"PASSWORD_{id}"] = password;
                    }

                    context.EnvironmentVariables["CONNECTIONS"] = AppendConnection(context, id);
                });
        }
    }

    private static string AppendConnection(EnvironmentCallbackContext context, string id)
    {
        if (!context.EnvironmentVariables.TryGetValue("CONNECTIONS", out var existing))
        {
            return id;
        }

        var connections = existing as string;

        if (string.IsNullOrEmpty(connections))
        {
            return id;
        }

        return $"{connections},{id}";
    }
}
