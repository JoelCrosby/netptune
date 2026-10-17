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

## Running Tests

The integration test suite uses [Testcontainers](https://dotnet.testcontainers.org/) and requires Docker.

```bash
cd server
dotnet test Netptune.IntegrationTests
```
