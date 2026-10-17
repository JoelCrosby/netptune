---
title: 'Kubernetes / Helm'
description: 'Deploy the current Netptune stack using the chart in this repository.'
---

The repository's maintained deployment definition is `charts/netptune`. It deploys the application services, stateful dependencies, Gateway API routes, and TLS resources in one Helm release.

## Prerequisites

- A Kubernetes cluster with a default `StorageClass`
- `kubectl` configured for the cluster
- Helm 3
- Traefik with Gateway API support and the `traefik.io` Middleware CRDs
- cert-manager with Gateway API HTTP-01 support when TLS is enabled
- DNS records for every enabled public host
- Credentials listed in [External Services](/docs/external-services)
- KEDA only if `parameters.activity.autoscaling.enabled` is set to `true`

The chart creates a `Gateway` with `gatewayClassName: traefik`; it does not install Traefik, cert-manager, or KEDA.

## 1. Clone the repository

```bash
git clone https://github.com/JoelCrosby/netptune.git
cd netptune
```

## 2. Create an override file

Keep deployment-specific values outside `charts/netptune/values.yaml`. Start with a private file such as `values.production.yaml`:

```yaml
ingress:
  host: netptune.example.com
  appHost: app.netptune.example.com
  apiHost: api.netptune.example.com
  tls:
    enabled: true
    email: admin@example.com

website:
  host: www.netptune.example.com

# Disable administrative surfaces that you are not configuring.
headlamp:
  enabled: false

dbgate:
  enabled: false

pgadmin:
  enabled: false

aspire:
  enabled: false

persistence:
  postgres:
    size: 40Gi
  meilisearch:
    size: 40Gi
  nats:
    size: 40Gi
```

The current route for `ingress.host` sends `/api/` to the app and `/` to the website service. `ingress.appHost` serves the Angular client and proxies `/api/` to the app. `ingress.apiHost` sends the complete host to the separately deployed API, including its `/docs` UI and `/openapi/v1.json` document.

:::warning

The API image currently defaults `Origin` and `CorsOrigins` to the project's own domains. Changing `ingress.appHost` alone is not sufficient for a custom-domain deployment; add `Origin` and `CorsOrigins__0` under `serverConfig` in the override file.

:::

## 3. Add secrets

Add the required values to the same private override file. Infrastructure passwords live under `secrets`; the chart derives the server connection strings from them. Server settings go under `serverSecrets` and `serverConfig`, keyed by their env-form configuration keys (see [Configuration](/docs/configuration)), and are shared by the app, API, jobs, and activity pods. The API validates all three OAuth provider configurations at startup, so every listed OAuth value must be non-empty.

```yaml
secrets:
  postgres:
    postgres_password: change-me

  cache:
    cache_password: change-me

  meilisearch:
    master_key: change-me-at-least-16-bytes

serverSecrets:
  Authentication__SigningKey: change-me-long-random-value
  Authentication__GitHub__ClientSecret: change-me
  Authentication__Google__ClientSecret: change-me
  Authentication__Microsoft__ClientSecret: change-me
  Storage__S3__BucketName: netptune
  Storage__S3__Region: us-east-1
  Storage__S3__AccessKeyId: change-me
  Storage__S3__SecretAccessKey: change-me
  Email__Cloudflare__ApiToken: change-me
  Turnstile__SecretKey: change-me

serverConfig:
  Authentication__GitHub__ClientId: change-me
  Authentication__GitHub__CallbackPath: /signin-github
  Authentication__Google__ClientId: change-me
  Authentication__Google__CallbackPath: /signin-google
  Authentication__Microsoft__ClientId: change-me
  Authentication__Microsoft__CallbackPath: /signin-microsoft
  Email__Cloudflare__AccountId: change-me
```

Anything else on the [Configuration](/docs/configuration) page can be added to `serverConfig` or `serverSecrets` the same way without changing the chart.

Generate random values with tools such as:

```bash
openssl rand -base64 64
openssl rand -hex 32
```

:::warning

Do not commit the override file. `charts/netptune/values.secret.yaml.example` contains the full shape and is a reasonable starting point; the project's own production deploy keeps the same file encrypted with SOPS and decrypts it in CI, but any private values file passed with `--values` works.

:::

If Aspire, Headlamp, or DbGate remain enabled, also populate their OAuth and cookie secrets under `secrets.aspire`, `secrets.headlamp`, and `secrets.dbgate`. To enable the Cloudflare edge check described in `charts/netptune/values.yaml`, add `TrustedProxies__EdgeAuthorizationSecret` to `serverSecrets`.

## 4. Review images and optional components

The chart defaults application images to `latest` with `imagePullPolicy: Always`. Override them with immutable SHA tags for production:

```yaml
parameters:
  api:
    api_image: ghcr.io/joelcrosby/netptune:sha-<full-commit-sha>
  api:
    api_image: ghcr.io/joelcrosby/netptune-api:sha-<full-commit-sha>
  jobs:
    jobs_image: ghcr.io/joelcrosby/netptune-jobs:sha-<full-commit-sha>
  activity:
    activity_image: ghcr.io/joelcrosby/netptune-activity:sha-<full-commit-sha>
  client:
    client_image: ghcr.io/joelcrosby/netptune-client:sha-<full-commit-sha>

website:
  image: ghcr.io/joelcrosby/netptune-website:sha-<full-commit-sha>
```

The `/api/v1` route is served by the independently scalable API deployment. Its API-key-only host shares PostgreSQL, Valkey, and NATS with the main application but has its own image, ConfigMap, service, and health probes, and shares the `server-secrets` and `server-config` values with the other server pods.

The production client image currently contains the Netptune-hosted Cloudflare Turnstile site key at build time. A self-hosted deployment needs a client image rebuilt with its own site key in `client/src/environments/environment.prod.ts`; the Helm chart only configures the server-side secret.

## 5. Render and install

Render the manifests first and inspect the result:

```bash
helm template netptune charts/netptune \
  --namespace netptune \
  --values values.production.yaml > rendered.yaml
```

Install or upgrade the release:

```bash
helm upgrade --install netptune charts/netptune \
  --namespace netptune \
  --create-namespace \
  --values values.production.yaml
```

## 6. Verify the deployment

```bash
kubectl get deployments,statefulsets,pods -n netptune
kubectl get gateway,httproute -n netptune
kubectl get certificate -n netptune
```

The core workload should include API, client, jobs, activity, website, PostgreSQL, Valkey, NATS, and Meilisearch. Check health and logs with:

```bash
kubectl rollout status deployment/api-deployment -n netptune
kubectl logs deployment/api-deployment -n netptune
kubectl logs deployment/jobs-deployment -n netptune
kubectl logs deployment/activity-deployment -n netptune
```

The API, jobs, and activity deployments expose `/health/live` and `/health/ready` probes.

## Activity retention and scaling

The chart schedules `activity-retention-cronjob` at `03:00` daily by default. It archives eligible audit entries to S3 before deleting them.

Activity autoscaling is disabled by default. Enabling it creates a KEDA `ScaledObject` driven by NATS JetStream consumer lag, so the KEDA CRDs and controller must already exist.

## Updating

Update the pinned image tags and run the same `helm upgrade --install` command. API, client, activity, and website use rolling deployments; the stateful dependencies use StatefulSets.
