// ReSharper disable UnusedVariable

using Netptune.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddKubernetesEnvironment("k8s")
    .WithProperties(k8s =>
    {
        k8s.HelmChartName = "netptune-app";
    });

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var postgresdb = postgres.AddDatabase("postgresdb", "netptune");

var kafka = builder
    .AddKafka("kafka")
    .WithKafkaUI()
    .WithDataVolume();

var cache = builder.AddValkey("cache");

var jobs = builder
    .AddProject<Projects.Netptune_JobServer>("jobs")
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithReference(kafka);

var api = builder
    .AddProject<Projects.Netptune_App>("api")
    .WithJobServer(jobs)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithReference(kafka);

var client = builder
    .AddViteApp("client", "../../client/", "start")
    .WithPnpm();

builder.Build().Run();
