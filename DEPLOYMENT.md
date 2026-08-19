# Deployment

## Prerequisites

- A Kubernetes cluster (tested on k3s)
- `helm` and `kubectl` configured for the cluster
- A PostgreSQL instance
- A Redis / Valkey instance
- A NATS server with JetStream enabled
- An S3-compatible bucket
- A SendGrid API key (for email)
- A GitHub OAuth app (for social login)

## Deploy with Helm

```bash
helm upgrade --install netptune-app charts/netptune/ \
  --namespace default \
  --set ingress.enabled=true \
  --set "ingress.host=your-domain.com" \
  --set "ingress.publicApiHost=api.your-domain.com" \
  --set ingress.tls.enabled=true \
  --set "ingress.tls.email=your@email.com" \
  --set "secrets.postgres.postgres_password=<password>" \
  --set "secrets.cache.cache_password=<password>" \
  --set "secrets.publicApi.cache_password=<password>" \
  --set "secrets.publicApi.postgres_password=<password>" \
  --set "secrets.api.signing_key=<jwt-signing-key>" \
  --set "secrets.api.github_client_id=<github-client-id>" \
  --set "secrets.api.github_secret=<github-secret>" \
  --set "secrets.api.sendgrid_api_key=<sendgrid-key>" \
  --set "secrets.api.s3_bucket_name=<bucket>" \
  --set "secrets.api.s3_region=<region>" \
  --set "secrets.api.s3_access_key_id=<key-id>" \
  --set "secrets.api.s3_secret_access_key=<secret>"
```

See [charts/netptune/values.yaml](charts/netptune/values.yaml) for the full set of configurable values.

## Traefik ingress

Traefik terminates TLS for the cluster and serves the Gateway API resources the app chart
creates. It is a **separate release in its own namespace**, not part of `netptune-app`, and
CI does not deploy it — changes here are applied by hand.

`charts/traefik/` wraps the upstream chart as a pinned dependency so its values are version
controlled rather than living only in the cluster.

```bash
helm dependency update charts/traefik/
helm upgrade --install traefik charts/traefik/ --namespace traefik
```

The release name must stay `traefik`. The upstream chart derives its resource names from it,
so renaming would orphan the running Deployment, Service and the Vultr load balancer that
Cloudflare points at.

The vendored subchart under `charts/traefik/charts/` is gitignored; `Chart.lock` pins the
version and `helm dependency update` restores it.

## Local Development

The server projects use [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) for local orchestration. Docker is required.

```bash
# Start the full backend stack (app API, public API, jobs, Postgres, Redis, NATS)
cd server
dotnet run --project Netptune.AppHost

# Start the Angular dev server
cd client
npm install
npm start
```

## Running Tests

The integration test suite uses [Testcontainers](https://dotnet.testcontainers.org/) and requires Docker.

```bash
cd server
dotnet test Netptune.IntegrationTests
```
