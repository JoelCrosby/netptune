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
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:7400")
    .WithJobServer(jobs)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithNats(nats)
    .WithExternalHttpEndpoints();

builder.Build().Run();
