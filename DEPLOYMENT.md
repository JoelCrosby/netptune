# Deployment

## Prerequisites

- A Kubernetes cluster (tested on k3s)
- `helm` and `kubectl` configured for the cluster
- A PostgreSQL instance
- A Redis / Valkey instance
- A NATS server with JetStream enabled
- An S3-compatible bucket
- A Cloudflare account with Email Sending (for email)
- GitHub, Google and Microsoft OAuth apps (for social login)

## Deploy with Helm

Every secret and per-environment setting goes in one values file, passed with `-f`. Its full shape,
with every key, is [charts/netptune/values.secret.yaml.example](charts/netptune/values.secret.yaml.example):

- **`secrets.*`** holds the infrastructure passwords and the oauth2-proxy apps (postgres, cache,
  meilisearch, Aspire, Headlamp, dbgate). The chart derives the application connection strings from
  them.
- **`serverSecrets`** and **`serverConfig`** are rendered as-is into the `server-secrets` Secret
  and the `server-config` ConfigMap, which every .NET server pod (app, api, jobs, activity) loads.
  Keys are .NET configuration keys in env form, for example `Storage__S3__SecretAccessKey` for
  `Storage:S3:SecretAccessKey`.

```bash
helm upgrade --install netptune-app charts/netptune/ \
  --namespace default \
  --values values.secret.yaml \
  --set ingress.enabled=true \
  --set "ingress.host=your-domain.com" \
  --set "ingress.apiHost=api.your-domain.com" \
  --set ingress.tls.enabled=true
```

The server validates its options at startup, so a missing required value stops the pod with an
error naming the key. See [charts/netptune/values.yaml](charts/netptune/values.yaml) for everything
else that can be configured.

## Production secrets (SOPS)

Production values live in the private repo **`JoelCrosby/netptune-secrets`**, as
`secrets.prod.enc.yaml` with its values encrypted by [SOPS](https://github.com/getsops/sops) using
an [age](https://github.com/FiloSottile/age) key. Nothing secret, encrypted or not, is committed to
this public repo; `.gitignore` refuses `charts/**/secrets.*.yaml` as a safety net.

The deploy workflow:

1. checks that repo out with a read-only deploy key (`SECRETS_REPO_DEPLOY_KEY`);
2. decrypts the file with `SOPS_AGE_KEY`;
3. fails on any empty value;
4. passes it to `helm upgrade -f`.

Those two and `KUBECONFIG_PROD` are the only Actions secrets the deploy reads.

**Adding or changing a value:**

1. In the netptune-secrets repo, run `sops secrets.prod.enc.yaml`. It opens decrypted in `$EDITOR`
   and re-encrypts on save. Add the key under `serverSecrets` (secret) or `serverConfig` (plain).
2. If no options class reads it yet, add the property in code here.
3. Commit and push the secrets repo. That alone triggers nothing here.
4. Run the **Deploy** workflow manually (Actions → Deploy → Run workflow, "force build" off). It
   redeploys the current images with the new values, and the checksum annotations restart the
   server pods.

If the change ships with code that reads the new key, push the secrets repo first. The normal push
deploy then picks up both.

**One-time setup:** `./setup.sh` in the netptune-secrets repo does all of this interactively. It can
also pre-fill the values from the live cluster's Secrets. The steps it performs:

1. Install `sops` and `age`.
2. Create the key with `age-keygen -o key.txt`. **Back up `key.txt` in a password manager**: losing it
   means the prod file can't be decrypted.
3. Create the private repo `JoelCrosby/netptune-secrets`, with a `.sops.yaml` holding the public key
   (`age-keygen -y key.txt`):

   ```yaml
   creation_rules:
     - path_regex: secrets\..*\.enc\.yaml$
       age: "age1…"
   ```

4. In that repo, build `secrets.prod.yaml` from this repo's `charts/netptune/values.secret.yaml.example`
   and fill in the values. Encrypt it with
   `sops --encrypt --filename-override secrets.prod.enc.yaml secrets.prod.yaml > secrets.prod.enc.yaml`.
   The override makes `.sops.yaml`'s `*.enc.yaml` rule apply. Delete the plain file, then commit and
   push the encrypted one. That repo's `.gitignore` already refuses plaintext.
5. Create the deploy key:
   - generate it with `ssh-keygen -t ed25519 -N "" -f netptune-secrets-deploy`;
   - add the `.pub` file as a **read-only** deploy key on netptune-secrets;
   - add the private file as the `SECRETS_REPO_DEPLOY_KEY` Actions secret on this repo;
   - delete both local files.
6. Add the full contents of `key.txt` as the `SOPS_AGE_KEY` Actions secret on this repo.
7. Keep the key at `~/.config/sops/age/keys.txt`, where `sops` looks by default, or set
   `SOPS_AGE_KEY_FILE`.

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
2. **`serverSecrets.TrustedProxies__EdgeAuthorizationSecret`** in the values file, set to that same
   value. When it is set, the chart adds the matching `TrustedProxies__EdgeAuthorizationHeader` to
   `server-config`, so every server pod gets the pair together.

Leave the secret empty and the check is skipped, which is what local development does. Set only one
half and the application refuses to start rather than quietly falling back — a half-configured check
would otherwise show up days later as rate limits behaving oddly.

Create the Transform Rule **before** setting the secret. A header nothing is sending is ignored, but a
secret nothing is stamping means the address header stops being believed and every caller collapses
into the ingress's own address for rate limiting.

Nothing here expires or needs refreshing. Rotating the secret means updating the Transform Rule and
the `serverSecrets` value; there is no scheduled maintenance. To rotate without a gap, add the new value to
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

### Cluster-parity mode

The `cluster-parity` launch profile brings up a stack that routes and runs like the cluster, for
reproducing problems that only show up there. The default profile is unchanged.

```bash
cd server
dotnet run --project Netptune.AppHost --launch-profile cluster-parity
```

It runs the client and website from their Dockerfiles behind a Traefik edge (the cluster's
`traefik:v3.6.13`) on port 8000. Its routes and middlewares in `server/Netptune.AppHost/Edge/`
mirror the chart's HTTPRoutes and `middleware.yaml`:

| Host                                 | Routes like       |
| ------------------------------------ | ----------------- |
| `http://netptune.localhost:8000`     | `ingress.host`    |
| `http://app.netptune.localhost:8000` | `ingress.appHost` |
| `http://api.netptune.localhost:8000` | `ingress.apiHost` |
| `http://www.netptune.localhost:8000` | `website.host`    |

Browsers resolve `*.localhost` to loopback and treat it as a secure context, so the `Secure` auth
cookies work over plain http.

Compared with the default profile:

- **Services.** app and api listen on 7400 and 7600, the pods' ports, bound to loopback. The profile
  turns on Aspire's container tunnel so the edge and the client's nginx can still reach them. All four
  servers run as `Production`, without the launch profiles or `appsettings.Development.json`. They
  get the chart's config keys as `Section__Key` env vars: forwarded headers, trusted proxies, Origin
  and CORS, the `Ai__*` limits and the OTLP retry. Turnstile uses Cloudflare's test keys.
  `DOTNET_GCHeapHardLimit` reproduces the heap cap a 512Mi pod limit imposes.
- **Edge.** Traefik stamps `X-Netptune-Edge` with a generated secret on every request, standing in
  for the Cloudflare Transform Rule. A `CF-Connecting-IP` sent through port 8000 is believed, so rate
  limits partition per address. Sent straight to 7400 it is ignored, as at an unprotected origin.
- **Backing services.** These are the same containers as the default profile, so data is shared.
  They get the chart's memory limits. Valkey gets `maxmemory 320mb` with `volatile-lru` and
  appendonly persistence, and Meilisearch gets `MEILI_ENV=production`. Switching profile recreates
  the containers but keeps their volumes. Valkey has no volume in the default profile, so its
  contents, including the DataProtection key ring, go with each switch and you have to sign in again.
- **No seeding.** seed-data is skipped. Each service creates the schema on startup, as in the
  cluster, so a fresh volume starts empty and you register a user.
- **Scheduled work.** `activity-retention` runs `--job retention` and `postgres-backup` runs the
  backup's `pg_dump` and verify step into the `netptune-postgres-backup` volume. Both are explicit
  start, so start them from the dashboard. Retention archives to whichever bucket the shell's
  `Storage__S3__*` settings name before deleting rows; the backup never uploads.

The cluster-parity mode builds container images. Aspire builds them with `docker build`, which
needs the buildx plugin. On a rootless Podman machine without it, set
`ASPIRE_CONTAINER_RUNTIME=podman` before running.

Not reproduced: TLS and the http→https redirect, HPAs and the KEDA `ScaledObject` (one instance of
each service), CPU requests, route `timeouts`, the dashboard's oauth2-proxy and telemetry caps, the
chart's exact image tags for the backing services (the persistent volumes are on Aspire's defaults),
and running the servers from their published images rather than as projects.

## Running Tests

The integration test suite uses [Testcontainers](https://dotnet.testcontainers.org/) and requires Docker.

```bash
cd server
dotnet test Netptune.IntegrationTests
```
