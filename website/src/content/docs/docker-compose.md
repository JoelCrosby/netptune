---
title: 'Docker Compose'
description: 'Deploy Netptune on a single machine using Docker Compose.'
---

Docker Compose is the simplest way to get a self-hosted Netptune instance running. All six services are defined in a single file and start with one command.

## Prerequisites

- Docker Engine 24+ and Docker Compose v2
- A domain name or static IP address
- Credentials for all [external services](/docs/external-services)

## Installation

1. **Create a working directory**

Create a directory to hold your Netptune deployment files.

```bash
mkdir netptune && cd netptune
```

You will create the following two files inside this directory:

```
netptune/
├── docker-compose.yml
└── .env
```

2. **Create the Compose file**

Create `docker-compose.yml` with the following content:

```yaml
services:
  client:
    image: ghcr.io/joelcrosby/netptune-client:latest
    restart: unless-stopped
    ports:
      - '80:80'
    depends_on:
      - api

  api:
    image: ghcr.io/joelcrosby/netptune:latest
    restart: unless-stopped
    environment:
      ASPNETCORE_URLS: http://0.0.0.0:7400
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: 'true'
      ConnectionStrings__netptune: 'Host=postgres;Port=5432;Username=postgres;Password=${POSTGRES_PASSWORD};Database=netptune'
      ConnectionStrings__cache: '${REDIS_PASSWORD}@cache:6379'
      ConnectionStrings__nats: 'nats://nats:4222'
      NETPTUNE_SIGNING_KEY: '${SIGNING_KEY}'
      NETPTUNE_GITHUB_CLIENT_ID: '${GITHUB_CLIENT_ID}'
      NETPTUNE_GITHUB_SECRET: '${GITHUB_SECRET}'
      SEND_GRID_API_KEY: '${SENDGRID_API_KEY}'
      NETPTUNE_S3_BUCKET_NAME: '${S3_BUCKET_NAME}'
      NETPTUNE_S3_REGION: '${S3_REGION}'
      NETPTUNE_S3_ACCESS_KEY_ID: '${S3_ACCESS_KEY_ID}'
      NETPTUNE_S3_SECRET_ACCESS_KEY: '${S3_SECRET_ACCESS_KEY}'
    depends_on:
      - postgres
      - cache
      - nats

  jobs:
    image: ghcr.io/joelcrosby/netptune-jobs:latest
    restart: unless-stopped
    environment:
      ConnectionStrings__netptune: 'Host=postgres;Port=5432;Username=postgres;Password=${POSTGRES_PASSWORD};Database=netptune'
      ConnectionStrings__cache: '${REDIS_PASSWORD}@cache:6379'
      ConnectionStrings__nats: 'nats://nats:4222'
      NETPTUNE_SIGNING_KEY: '${SIGNING_KEY}'
      SEND_GRID_API_KEY: '${SENDGRID_API_KEY}'
      NETPTUNE_S3_BUCKET_NAME: '${S3_BUCKET_NAME}'
      NETPTUNE_S3_REGION: '${S3_REGION}'
      NETPTUNE_S3_ACCESS_KEY_ID: '${S3_ACCESS_KEY_ID}'
      NETPTUNE_S3_SECRET_ACCESS_KEY: '${S3_SECRET_ACCESS_KEY}'
    depends_on:
      - postgres
      - cache
      - nats

  postgres:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: '${POSTGRES_PASSWORD}'
      POSTGRES_DB: netptune
      POSTGRES_HOST_AUTH_METHOD: scram-sha-256
      POSTGRES_INITDB_ARGS: '--auth-host=scram-sha-256 --auth-local=scram-sha-256'
    volumes:
      - postgres_data:/var/lib/postgresql/data

  cache:
    image: valkey/valkey:9-alpine
    restart: unless-stopped
    command: ['valkey-server', '--requirepass', '${REDIS_PASSWORD}']
    volumes:
      - cache_data:/data

  nats:
    image: nats:alpine
    restart: unless-stopped
    command: ['-js']

volumes:
  postgres_data:
  cache_data:
```

3. **Create the environment file**

Create a `.env` file in the same directory. Never commit this file to source control.

```env
# Database
POSTGRES_PASSWORD=change_me_strong_password

# Cache
REDIS_PASSWORD=change_me_redis_password

# JWT signing key — generate with: openssl rand -base64 64
SIGNING_KEY=change_me_very_long_random_signing_key

# GitHub OAuth (optional — leave blank to disable GitHub login)
GITHUB_CLIENT_ID=
GITHUB_SECRET=

# SendGrid
SENDGRID_API_KEY=SG.xxxxxx

# S3-compatible storage
S3_BUCKET_NAME=netptune
S3_REGION=us-east-1
S3_ACCESS_KEY_ID=
S3_SECRET_ACCESS_KEY=
```

Generate a secure signing key with:

```bash
openssl rand -base64 64
```

4. **Start the stack**

Pull all images and start the services in the background:

```bash
docker compose up -d
```

Check that all six containers are running:

```bash
docker compose ps
```

You should see `client`, `api`, `jobs`, `postgres`, `cache`, and `nats` all in the `running` state.

5. **Verify the deployment**

Tail the API logs to confirm it started and connected to the database:

```bash
docker compose logs -f api
```

Look for output similar to:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:7400
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

Open your browser and navigate to `http://<your-server-ip>` to access the Netptune UI.

6. **Configure TLS (recommended)**

For production use, place a reverse proxy in front of the client container to terminate TLS. A minimal Caddy setup will automatically obtain and renew a Let's Encrypt certificate:

```caddyfile
your-domain.com {
    reverse_proxy localhost:80
}
```

:::note

Run Caddy alongside your stack and point your domain's DNS A record at the server. Caddy handles ACME challenges and certificate renewal automatically.

:::

## Updating

To update to the latest images, pull and restart. Database migrations run automatically on API startup — no manual steps are required.

```bash
docker compose pull
docker compose up -d
```

## Stopping and removing

Stop without removing data:

```bash
docker compose down
```

Stop and remove all data volumes (destructive):

```bash
docker compose down -v
```

## Troubleshooting

### API fails to start

Check the connection strings. The most common cause is an incorrect password or hostname in `ConnectionStrings__netptune`.

```bash
docker compose logs api
```

### Cannot connect to the database

Ensure PostgreSQL is healthy before the API starts. Add a health check to the `postgres` service:

```yaml
postgres:
  healthcheck:
    test: ['CMD-SHELL', 'pg_isready -U postgres']
    interval: 5s
    timeout: 5s
    retries: 5
```

Then update the `api` and `jobs` `depends_on` blocks to use `condition: service_healthy`.

### File uploads fail

Verify your S3 credentials and confirm the bucket exists with the correct region set.

### Emails are not sent

Check that your SendGrid API key is valid and your sender address is verified in the SendGrid dashboard. See the [External Services](/docs/external-services) guide.
