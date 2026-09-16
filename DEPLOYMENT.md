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
  --set "ingress.apiHost=api.your-domain.com" \
  --set ingress.tls.enabled=true \
  --set "ingress.tls.email=your@email.com" \
  --set "secrets.postgres.postgres_password=<password>" \
  --set "secrets.cache.cache_password=<password>" \
  --set "secrets.api.cache_password=<password>" \
  --set "secrets.api.postgres_password=<password>" \
  --set "secrets.app.signing_key=<jwt-signing-key>" \
  --set "secrets.app.github_client_id=<github-client-id>" \
  --set "secrets.app.github_secret=<github-secret>" \
  --set "secrets.app.sendgrid_api_key=<sendgrid-key>" \
  --set "secrets.app.s3_bucket_name=<bucket>" \
  --set "secrets.app.s3_region=<region>" \
  --set "secrets.app.s3_access_key_id=<key-id>" \
  --set "secrets.app.s3_secret_access_key=<secret>"
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

## Client addresses and the edge

Rate limiting partitions on the caller's address, so the application has to know which address to
believe. Requests arrive from the ingress, not from the caller, and the caller's own address comes in
a header the CDN adds. Anyone who reaches the origin without going through the CDN can set that
header to whatever they like, which would let them mint a fresh rate limit partition per request.

The origin has a public address and answers on it, so being behind Cloudflare is not by itself
evidence that a given request came through Cloudflare. The application therefore believes the address
header only when the request also carries a secret that only the edge knows.

Two things have to agree:

1. A **Cloudflare Transform Rule** — Rules → Transform Rules → Modify Request Header → *Set static*,
   applied to all requests, setting the header named by `trustedProxies.edgeAuthorizationHeader`
   (default `X-Netptune-Edge`) to a generated secret.
2. The **`NETPTUNE_EDGE_AUTHORIZATION_SECRET`** GitHub Actions secret, set to that same value. The
   deploy workflow passes it to both `secrets.app.edge_authorization_secret` and
   `secrets.api.edge_authorization_secret`. For a manual `helm upgrade`, set those two values
   directly in `values.secret.yaml` instead.

Leave the secret empty and the check is skipped, which is what local development does. Set only one
half and the application refuses to start rather than quietly falling back — a half-configured check
would otherwise show up days later as rate limits behaving oddly.

Create the Transform Rule **before** setting the secret. A header nothing is sending is ignored, but a
secret nothing is stamping means the address header stops being believed and every caller collapses
into the ingress's own address for rate limiting.

Nothing here expires or needs refreshing. Rotating the secret means updating the Transform Rule and
the GitHub secret; there is no scheduled maintenance. To rotate without a gap, add the new value to
the Transform Rule first, deploy, then remove the old one.

Swapping CDN means changing `trustedProxies.clientAddressHeader` to whatever that CDN uses
(`True-Client-IP` for Akamai and Fastly) and recreating the equivalent header rule. No application
code changes.

### Why not an origin IP allowlist

Restricting the load balancer to Cloudflare's published ranges is the more usual advice, and
`charts/traefik/values.yaml` documents how. It is not used here because the range list has to be
refreshed by hand and a stale list refuses real traffic — it makes continued operation depend on a
recurring manual task. It remains worth adding if you later want the origin closed for DDoS or
WAF-bypass reasons, which the edge secret does not address.

## Local Development

The server projects use [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) for local orchestration. Docker is required.

```bash
# Start the full backend stack (app, API, jobs, Postgres, Redis, NATS)
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
