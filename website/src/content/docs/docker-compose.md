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

The AppHost starts PostgreSQL, Valkey, NATS with JetStream, Meilisearch, seed data, the app, the API-key-only API, jobs, and activity projects. Service credentials used by the APIs and workers still need to be supplied through .NET user secrets or environment variables; see the [Configuration Reference](/docs/configuration).

Run the Angular client separately:

```bash
cd client
pnpm install
pnpm start
```

The development client runs on `http://localhost:6400`. The app launch profile uses `http://localhost:7400` and `https://localhost:7401`; the API profile uses `http://localhost:7500` and `https://localhost:7501`.

### Cluster-parity mode

To reproduce problems that only appear in the cluster, start the AppHost with its `cluster-parity` launch profile:

```bash
dotnet run --project server/Netptune.AppHost/Netptune.AppHost.csproj --launch-profile cluster-parity
```

This builds the client and website from their Dockerfiles and serves everything through a Traefik edge on port 8000. The edge reproduces the chart's Gateway routes on `netptune.localhost`, `app.netptune.localhost`, `api.netptune.localhost` and `www.netptune.localhost`. The app and API run as `Production` on ports 7400 and 7600, with the chart's forwarded-header, trusted-proxy and memory settings. Seed data is not loaded. `DEPLOYMENT.md` lists what the mode covers and what it leaves out.

## Production deployment

Use the [Kubernetes / Helm guide](/docs/kubernetes) for the deployment path represented by the current repository.
