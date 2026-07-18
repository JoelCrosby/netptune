---
title: 'Self-Hosting Guide'
description: 'The current architecture, dependencies, and supported deployment path for Netptune.'
---

Netptune is open source and can be run on your own infrastructure. The repository currently provides a Kubernetes Helm chart as its deployment definition. A supported Docker Compose stack is not included.

:::tip

For a production deployment, start with the [Kubernetes / Helm guide](/docs/kubernetes). The [Docker Compose page](/docs/docker-compose) explains the current status of single-machine deployments.

:::

## Architecture

### Application services

| Service    | Image                                    | Responsibility                                                                                 |
| ---------- | ---------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Client     | `ghcr.io/joelcrosby/netptune-client`     | Angular application served by Nginx. Nginx proxies `/api/` to the API service.                 |
| API        | `ghcr.io/joelcrosby/netptune`            | ASP.NET Core API, authentication, business logic, file access, search, and server-sent events. |
| Public API | `ghcr.io/joelcrosby/netptune-public-api` | API-key-only integration surface with OpenAPI documentation and independent scaling.           |
| Jobs       | `ghcr.io/joelcrosby/netptune-jobs`       | Email delivery, search indexing, and automation processing.                                    |
| Activity   | `ghcr.io/joelcrosby/netptune-activity`   | Activity event processing, merge windows, reporting events, and scheduled audit retention.     |

The chart can also deploy `ghcr.io/joelcrosby/netptune-website`, the SolidStart marketing and documentation site. It is separate from the Angular application.

### Stateful dependencies

| Dependency            | Current chart image          | Purpose                                                                       |
| --------------------- | ---------------------------- | ----------------------------------------------------------------------------- |
| PostgreSQL            | `postgres:18`                | Primary relational database.                                                  |
| Valkey                | `valkey/valkey:9.0`          | Distributed cache and data-protection storage.                                |
| NATS                  | `nats:2.14.3-alpine`         | JetStream-backed event delivery between the API, jobs, and activity services. |
| Meilisearch           | `getmeili/meilisearch:v1.10` | Workspace search indexes.                                                     |
| S3-compatible storage | External                     | Uploaded media and archived audit data.                                       |

PostgreSQL, NATS, and Meilisearch use persistent volume claims in the chart. Valkey is currently deployed without a persistent volume.

### Required external services

- Cloudflare Email Sending for transactional email
- Cloudflare Turnstile for login and registration challenges
- S3-compatible object storage
- GitHub, Google, and Microsoft OAuth credentials

See [External Services](/docs/external-services) for the values expected by the current server.

### Optional operational services

The chart also contains optional or administrative components:

- Aspire Dashboard for OpenTelemetry data
- Headlamp for Kubernetes administration
- DbGate or pgAdmin for database administration
- KEDA-based scaling for the activity consumer when explicitly enabled

Headlamp, DbGate, and Aspire Dashboard are enabled in the current default values. Review and disable anything you do not intend to expose before installing the chart.

## Container images

Images are published to GitHub Container Registry by manually dispatched workflows on the default branch.

| Component  | Image                                    |
| ---------- | ---------------------------------------- |
| API        | `ghcr.io/joelcrosby/netptune`            |
| Public API | `ghcr.io/joelcrosby/netptune-public-api` |
| Client     | `ghcr.io/joelcrosby/netptune-client`     |
| Jobs       | `ghcr.io/joelcrosby/netptune-jobs`       |
| Activity   | `ghcr.io/joelcrosby/netptune-activity`   |
| Website    | `ghcr.io/joelcrosby/netptune-website`    |

The workflows publish `latest`, branch, and full commit-SHA tags. Pin SHA tags for repeatable production deployments.

## Choose a deployment path

:::cards

- [Kubernetes / Helm](/docs/kubernetes) — the deployment definition maintained in this repository.
- [Docker Compose](/docs/docker-compose) — current limitations and the supported contributor workflow.

:::
