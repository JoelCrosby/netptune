---
title: 'Kubernetes / Helm'
description: 'Deploy Netptune to a Kubernetes cluster using the official Helm chart.'
---

The Helm chart is the recommended deployment method for production. It manages all Kubernetes resources, handles secret injection, and supports TLS via cert-manager with automatic Let's Encrypt certificate renewal.

## Prerequisites

- A running Kubernetes cluster (k3s, EKS, GKE, etc.)
- `kubectl` configured with cluster access
- `helm` 3.x installed
- [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/) installed in the cluster
- [cert-manager](https://cert-manager.io/) installed for automatic TLS
- A domain name pointed at your cluster's external IP or load balancer
- Credentials for all [external services](/docs/external-services)

## Installation

1. **Clone the repository**

The Helm chart lives in the `charts/` directory of the Netptune repository:

```bash
git clone https://github.com/JoelCrosby/netptune.git
cd netptune
```

2. **Review default values**

Inspect `charts/netptune/values.yaml` and note the key sections you will need to override:

```yaml
ingress:
  enabled: true
  host: 'netptune.co.uk' # replace with your domain
  tls:
    enabled: true
    email: 'your@email.com' # used for Let's Encrypt registration

persistence:
  postgres:
    size: '40Gi' # adjust to your storage needs

parameters:
  api:
    api_image: 'ghcr.io/joelcrosby/netptune:latest'
    port_http: 7400
  jobs:
    jobs_image: 'ghcr.io/joelcrosby/netptune-jobs:latest'
    port_http: 8080
  client:
    client_image: 'ghcr.io/joelcrosby/netptune-client:latest'
    api_url: 'http://api-service:7400'
```

3. **Create a secrets file**

Copy the example secrets file and fill in your credentials:

```bash
cp charts/netptune/values.secret.yaml.example charts/netptune/values.secret.yaml
```

Edit `values.secret.yaml`:

```yaml
secrets:
  postgres:
    postgres_password: 'change_me_strong_password'

  cache:
    cache_password: 'change_me_redis_password'

  api:
    # Generate with: openssl rand -base64 64
    signing_key: 'change_me_signing_key'

    # GitHub OAuth (leave blank to disable)
    github_client_id: ''
    github_secret: ''

    # SendGrid
    sendgrid_api_key: 'SG.xxxxxx'

    # S3-compatible storage
    s3_bucket_name: 'netptune'
    s3_region: 'us-east-1'
    s3_access_key_id: ''
    s3_secret_access_key: ''
```

:::warning

Never commit `values.secret.yaml` to source control. Add it to your `.gitignore`.

:::

4. **Install the chart**

Run `helm upgrade --install` to deploy or upgrade the release:

```bash
helm upgrade --install netptune-app charts/netptune/ \
  --namespace netptune \
  --create-namespace \
  --values charts/netptune/values.yaml \
  --values charts/netptune/values.secret.yaml \
  --set ingress.host=your-domain.com \
  --set ingress.tls.email=your@email.com
```

Alternatively, pass secrets directly on the command line (useful for CI/CD pipelines):

```bash
helm upgrade --install netptune-app charts/netptune/ \
  --namespace netptune \
  --create-namespace \
  --set ingress.host=your-domain.com \
  --set ingress.tls.email=your@email.com \
  --set secrets.postgres.postgres_password="<password>" \
  --set secrets.cache.cache_password="<password>" \
  --set secrets.api.signing_key="<signing-key>" \
  --set secrets.api.sendgrid_api_key="<sendgrid-key>" \
  --set secrets.api.s3_bucket_name="<bucket>" \
  --set secrets.api.s3_region="<region>" \
  --set secrets.api.s3_access_key_id="<key-id>" \
  --set secrets.api.s3_secret_access_key="<secret>"
```

5. **Verify the deployment**

Check that all pods are running:

```bash
kubectl get pods -n netptune
```

Expected output — all pods should show `Running`:

```
NAME                       READY   STATUS    RESTARTS   AGE
api-xxxxxxxxx-xxxxx        1/1     Running   0          2m
jobs-xxxxxxxxx-xxxxx       1/1     Running   0          2m
client-xxxxxxxxx-xxxxx     1/1     Running   0          2m
postgres-0                 1/1     Running   0          2m
cache-0                    1/1     Running   0          2m
nats-xxxxxxxxx-xxxxx       1/1     Running   0          2m
```

Check the ingress for the assigned address:

```bash
kubectl get ingress -n netptune
```

Once the `ADDRESS` field is populated and DNS has propagated, Netptune will be accessible at `https://your-domain.com`.

6. **Verify TLS**

cert-manager will automatically request a Let's Encrypt certificate. Check the certificate status:

```bash
kubectl get certificate -n netptune
```

The `READY` column should show `True` within a few minutes.

## Kubernetes resources

The Helm chart deploys the following resources:

| Resource           | Kind          | Description                                   |
| ------------------ | ------------- | --------------------------------------------- |
| `api`              | Deployment    | ASP.NET Core API server                       |
| `jobs`             | Deployment    | Background job server                         |
| `client`           | Deployment    | Angular frontend + Nginx                      |
| `nats`             | Deployment    | NATS event messaging (JetStream enabled)      |
| `postgres`         | StatefulSet   | PostgreSQL with persistent volume             |
| `cache`            | StatefulSet   | Valkey / Redis with persistent volume         |
| `*-service`        | Service       | ClusterIP services for internal communication |
| `netptune-ingress` | Ingress       | NGINX Ingress with TLS termination            |
| `letsencrypt`      | ClusterIssuer | cert-manager issuer for Let's Encrypt         |
| `*-secret`         | Secret        | Passwords and API keys                        |
| `*-config`         | ConfigMap     | Non-sensitive configuration                   |

## Optional monitoring stack

The chart includes optional monitoring components. Enable them in `values.yaml`:

```yaml
monitoring:
  prometheus:
    enabled: false # metrics collection
  grafana:
    enabled: false # dashboards
  loki:
    enabled: false # log aggregation
  otelCollector:
    enabled: false # distributed tracing
```

A PgAdmin instance for database inspection can also be enabled:

```yaml
pgadmin:
  enabled: false
  email: 'admin@your-domain.com'
  password: 'change_me'
```

## Updating

Re-run the `helm upgrade` command with your values files. Database migrations run automatically when the API pod starts.

```bash
helm upgrade netptune-app charts/netptune/ \
  --namespace netptune \
  --values charts/netptune/values.yaml \
  --values charts/netptune/values.secret.yaml
```

## Uninstalling

```bash
helm uninstall netptune-app -n netptune
```

This removes all Kubernetes resources but preserves persistent volumes. To delete volumes as well:

```bash
kubectl delete pvc --all -n netptune
```

## Troubleshooting

### Pods stuck in Pending

Check events for the pod:

```bash
kubectl describe pod <pod-name> -n netptune
```

Common causes: insufficient cluster resources, or a PersistentVolumeClaim that cannot be satisfied because no StorageClass is available.

### API pod crashes on startup

```bash
kubectl logs deployment/api -n netptune
```

Verify all required secrets are present and the PostgreSQL StatefulSet is healthy before the API deployment starts.

### Certificate not issued

```bash
kubectl describe certificate -n netptune
kubectl describe clusterissuer letsencrypt -n netptune
```

Ensure the ingress host DNS record resolves to your cluster's external IP before cert-manager can complete the ACME challenge.

### Ingress has no address

Confirm the NGINX Ingress Controller is installed and running:

```bash
kubectl get pods -n ingress-nginx
```
