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

        public IResourceBuilder<T> WithKafka(IResourceBuilder<KafkaServerResource> kafka)
        {
            return builder
                .WithReference(kafka);
        }

        public IResourceBuilder<T> WithJobServer(IResourceBuilder<ProjectResource> jobs)
        {
            return builder
                .WaitFor(jobs)
                .WithReference(jobs);
        }
    }
}
