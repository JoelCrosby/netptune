// ReSharper disable UnusedVariable

using Netptune.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddKubernetesEnvironment("k8s")
    .WithProperties(options =>
    {
        options.HelmChartName = "netptune-app";
    });

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb()

    .WithHostPort(5432)
    .WithExternalHttpEndpoints()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresdb = postgres.AddDatabase("postgresdb", "netptune");

var nats = builder
    .AddNats("nats")
    .WithJetStream()
    .WithDataVolume()
    .WithOtlpExporter()
    .WithLifetime(ContainerLifetime.Persistent);

var cache = builder
    .AddValkey("cache")
    .WithOtlpExporter()
    .WithLifetime(ContainerLifetime.Persistent);

var jobs = builder
    .AddProject<Projects.Netptune_JobServer>("jobs")
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats);

var api = builder
    .AddProject<Projects.Netptune_App>("api")
    .WithJobServer(jobs)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WithExternalHttpEndpoints();

builder.Build().Run();
