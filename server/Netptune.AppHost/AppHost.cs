// ReSharper disable UnusedVariable

using Netptune.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddKubernetesEnvironment("k8s")
    .WithProperties(options =>
    {
        options.HelmChartName = "netptune-app";
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
    .WithKafka(kafka);

var api = builder
    .AddProject<Projects.Netptune_App>("api")
    .WithJobServer(jobs)
    .WithCache(cache)
    .WithPostgres(postgresdb)
    .WithKafka(kafka)
    .WithExternalHttpEndpoints();

var client = builder
    .AddViteApp("client", "../../client/", "start")
    .WithPnpm()
    .WithExternalHttpEndpoints();

builder.Build().Run();
