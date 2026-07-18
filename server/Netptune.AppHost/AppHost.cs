// ReSharper disable UnusedVariable

using Netptune.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb()
    .WithDbGate()

    .WithHostPort(5432)
    .WithExternalHttpEndpoints()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresdb = postgres.AddDatabase("postgresdb", "netptune");

var nats = builder
    .AddNats("nats")
    .WithJetStream()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var cache = builder
    .AddValkey("cache")
    .WithLifetime(ContainerLifetime.Persistent);

var meilisearch = builder
    .AddMeilisearch("meilisearch")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var seedData = builder
    .AddProject<Projects.Netptune_SeedData>("seed-data")
    .WithPostgres(postgresdb);

var jobs = builder
    .AddProject<Projects.Netptune_JobServer>("jobs")
    .WaitForCompletion(seedData)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WaitFor(meilisearch)
    .WithReference(meilisearch);

var activity = builder
    .AddProject<Projects.Netptune_ActivityServer>("activity")
    .WaitForCompletion(seedData)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats);

var api = builder
    .AddProject<Projects.Netptune_App>("api")
    .WaitForCompletion(seedData)
    .WithJobServer(jobs)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WaitFor(meilisearch)
    .WithReference(meilisearch)
    .WithExternalHttpEndpoints();

var publicApi = builder
    .AddProject<Projects.Netptune_PublicApi>("public-api")
    .WaitForCompletion(seedData)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WithExternalHttpEndpoints();

builder.Build().Run();
