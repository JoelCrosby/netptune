// ReSharper disable UnusedVariable

using Netptune.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder
    .AddValkey("cache")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .WithDbGate(dbGate => dbGate.WithValkey(cache))

    .WithHostPort(5432)
    .WithExternalHttpEndpoints()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresdb = postgres.AddDatabase("postgresdb", "netptune");

var nats = builder
    .AddNats("nats")
    .WithJetStream()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var meilisearch = builder
    .AddMeilisearch("meilisearch")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var seedData = builder.AddSeedData(postgresdb);

var jobs = builder
    .AddNetptuneProject<Projects.Netptune_JobServer>("jobs")
    .WaitForSeedData(seedData)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WaitFor(meilisearch)
    .WithReference(meilisearch);

var activity = builder
    .AddNetptuneProject<Projects.Netptune_ActivityServer>("activity")
    .WaitForSeedData(seedData)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats);

var app = builder
    .AddNetptuneProject<Projects.Netptune_App>("app", ClusterParity.AppPort)
    .WaitForSeedData(seedData)
    .WithJobServer(jobs)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WaitFor(meilisearch)
    .WithReference(meilisearch)
    .WithExternalHttpEndpoints();

var api = builder
    .AddNetptuneProject<Projects.Netptune_Api>("api", ClusterParity.ApiPort)
    .WaitForSeedData(seedData)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WaitFor(meilisearch)
    .WithReference(meilisearch)
    .WithExternalHttpEndpoints();

builder.AddClusterParity(new NetptuneResources
{
    Cache = cache,
    Postgres = postgres,
    PostgresDatabase = postgresdb,
    Nats = nats,
    Meilisearch = meilisearch,
    Jobs = jobs,
    Activity = activity,
    App = app,
    Api = api,
});

builder.Build().Run();
