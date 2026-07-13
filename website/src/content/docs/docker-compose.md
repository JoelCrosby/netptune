---
title: 'Docker Compose'
description: 'Current support status for running Netptune on a single machine.'
---

The repository does not currently contain a Docker Compose file. The previous documentation described a stack that no longer matched the application and omitted the activity service, Meilisearch, persistent JetStream storage, Cloudflare email, and Turnstile.

:::warning

There is no maintained or supported Compose deployment at this time. Do not use the old six-container example for a production instance.

:::

## Why the old stack is no longer sufficient

A current deployment needs all of the following:

- Angular client
- ASP.NET Core API
- jobs service
- activity service
- PostgreSQL
- password-protected Valkey
- NATS with JetStream and durable storage
- Meilisearch with a master key and durable storage
- S3-compatible object storage
- Cloudflare Email Sending credentials
- Cloudflare Turnstile configuration
- GitHub, Google, and Microsoft OAuth configuration

The maintained Helm chart also configures health probes, Gateway API routing, TLS certificates, request limits, optional telemetry, autoscaling, and an activity-retention CronJob. A new Compose definition needs equivalent service configuration before it can be documented as a supported deployment.

## Contributor workflow

The backend is orchestrated locally with .NET Aspire rather than Compose. You need .NET SDK 10 and a Docker-compatible container runtime.

```bash
dotnet run --project server/Netptune.AppHost/Netptune.AppHost.csproj
```

The AppHost starts PostgreSQL, Valkey, NATS with JetStream, Meilisearch, seed data, the API, jobs, and activity projects. Service credentials used by the API and workers still need to be supplied through .NET user secrets or environment variables; see the [Configuration Reference](/docs/configuration).

Run the Angular client separately:

```bash
cd client
pnpm install
pnpm start
```

The development client runs on `http://localhost:6400` and the API launch profile uses `http://localhost:7400` and `https://localhost:7401`.

## Production deployment

Use the [Kubernetes / Helm guide](/docs/kubernetes) for the deployment path represented by the current repository.
