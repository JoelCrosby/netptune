---
title: 'Self-Hosting Guide'
description: 'Everything you need to run Netptune on your own infrastructure.'
---

Netptune is fully open source and designed to be self-hosted. You own your data, your instance, and your deployment. This guide covers the complete process from choosing a deployment method through to a running production instance.

:::tip

If you just want to get up and running quickly on a single machine, head straight to the [Docker Compose](/docs/docker-compose) guide.

:::

## Architecture

Netptune is composed of three application services backed by three infrastructure dependencies.

### Application services

| Service | Image                                | Description                                                                          |
| ------- | ------------------------------------ | ------------------------------------------------------------------------------------ |
| client  | `ghcr.io/joelcrosby/netptune-client` | Angular frontend served by Nginx. Proxies `/api/` requests to the API server.        |
| api     | `ghcr.io/joelcrosby/netptune`        | ASP.NET Core REST API. Handles authentication, business logic, and real-time events. |
| jobs    | `ghcr.io/joelcrosby/netptune-jobs`   | Background job processor for async work such as email dispatch and event processing. |

### Infrastructure dependencies

| Dependency            | Purpose                                                                 | Required |
| --------------------- | ----------------------------------------------------------------------- | -------- |
| PostgreSQL 17         | Primary database                                                        | Yes      |
| Redis / Valkey 9      | Caching and session storage                                             | Yes      |
| NATS                  | Internal event messaging between services. Must have JetStream enabled. | Yes      |
| S3-compatible storage | File attachments (AWS S3, MinIO, etc.)                                  | Yes      |
| SendGrid              | Transactional email for invites and notifications                       | Yes      |
| GitHub OAuth App      | Social login via GitHub                                                 | Optional |
| Google OAuth App      | Social login via Google                                                 | Optional |
| Microsoft OAuth App   | Social login via Microsoft                                              | Optional |

## Prerequisites

### Accounts and credentials

Before deploying, ensure you have the following external accounts and credentials ready:

- SendGrid account with an API key and a verified sender email address
- S3-compatible storage — an AWS S3 bucket or a self-hosted MinIO instance
- GitHub, Google, or Microsoft OAuth App credentials (optional, for social login)

### Infrastructure

- A Linux server or Kubernetes cluster with at least 2 vCPUs and 2 GB RAM
- A domain name pointed at your server or load balancer
- A TLS certificate — Let's Encrypt is supported automatically in the Kubernetes deployment

### Tools

| Deployment method | Required tools             |
| ----------------- | -------------------------- |
| Docker Compose    | `docker`, `docker compose` |
| Kubernetes        | `kubectl`, `helm` 3.x      |

## Container images

All images are published to the GitHub Container Registry and are publicly available. Pinning to a specific git SHA tag is recommended for production deployments.

| Image                                | Tag      |
| ------------------------------------ | -------- |
| `ghcr.io/joelcrosby/netptune-client` | `latest` |
| `ghcr.io/joelcrosby/netptune`        | `latest` |
| `ghcr.io/joelcrosby/netptune-jobs`   | `latest` |

## Choose a deployment method

:::cards

- [Docker Compose](/docs/docker-compose) — simple single-machine deployment, best for homelabs and small teams.
- [Kubernetes / Helm](/docs/kubernetes) — production-grade deployment with rolling updates and persistent storage management.

:::
