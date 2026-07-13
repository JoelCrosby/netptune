---
title: 'Configuration Reference'
description: 'All environment variables and settings for every Netptune service.'
---

Netptune services are configured entirely through environment variables. The sections below document every option for each service. For Helm deployments, see the mapping table at the bottom of this page.

## API server

### Connection strings

Passed as environment variables using ASP.NET Core's double-underscore (`__`) separator for nested configuration keys.

| Variable                      | Example                                                                       | Description                                                              |
| ----------------------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| ConnectionStrings\_\_netptune | `Host=postgres;Port=5432;Username=postgres;Password=secret;Database=netptune` | PostgreSQL connection string. The database must exist and be accessible. |
| ConnectionStrings\_\_cache    | `password@cache:6379`                                                         | Redis / Valkey connection string.                                        |
| ConnectionStrings\_\_nats     | `nats://nats:4222`                                                            | NATS server URL. JetStream must be enabled on the server.                |

### Authentication

| Variable                     | Required | Description                                                                                                |
| ---------------------------- | -------- | ---------------------------------------------------------------------------------------------------------- |
| NETPTUNE_SIGNING_KEY         | Yes      | Secret key used to sign JWT tokens. Must be a long random string. Generate with `openssl rand -base64 64`. |
| NETPTUNE_GITHUB_CLIENT_ID    | No       | GitHub OAuth App client ID. Leave blank to disable GitHub login.                                           |
| NETPTUNE_GITHUB_SECRET       | No       | GitHub OAuth App client secret.                                                                            |
| NETPTUNE_GOOGLE_CLIENT_ID    | No       | Google OAuth client ID. Leave blank to disable Google login.                                               |
| NETPTUNE_GOOGLE_SECRET       | No       | Google OAuth client secret.                                                                                |
| NETPTUNE_MICROSOFT_CLIENT_ID | No       | Microsoft OAuth App client ID. Leave blank to disable Microsoft login.                                     |
| NETPTUNE_MICROSOFT_SECRET    | No       | Microsoft OAuth App client secret.                                                                         |

### Token behaviour

| Variable             | Default          | Description                            |
| -------------------- | ---------------- | -------------------------------------- |
| Tokens\_\_Issuer     | `netptune.co.uk` | JWT issuer claim.                      |
| Tokens\_\_Audience   | `netptune.co.uk` | JWT audience claim.                    |
| Tokens\_\_ExpireDays | `5`              | Number of days before a token expires. |

### Email

| Variable                        | Required | Description                                                               |
| ------------------------------- | -------- | ------------------------------------------------------------------------- |
| SEND_GRID_API_KEY               | Yes      | SendGrid API key used to send transactional emails.                       |
| Email\_\_DefaultFromAddress     | Yes      | The `From` address for all outgoing emails. Must be verified in SendGrid. |
| Email\_\_DefaultFromDisplayName | No       | Display name shown alongside the from address.                            |

### S3 storage

| Variable                      | Required | Description                                                        |
| ----------------------------- | -------- | ------------------------------------------------------------------ |
| NETPTUNE_S3_BUCKET_NAME       | Yes      | Name of the S3 bucket for file attachments.                        |
| NETPTUNE_S3_REGION            | Yes      | AWS region (e.g. `us-east-1`) or custom endpoint region for MinIO. |
| NETPTUNE_S3_ACCESS_KEY_ID     | Yes      | AWS access key ID or MinIO access key.                             |
| NETPTUNE_S3_SECRET_ACCESS_KEY | Yes      | AWS secret access key or MinIO secret key.                         |

### ASP.NET Core

| Variable                            | Example               | Description                                                                                                                          |
| ----------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| ASPNETCORE_URLS                     | `http://0.0.0.0:7400` | Addresses the API listens on.                                                                                                        |
| ASPNETCORE_FORWARDEDHEADERS_ENABLED | `true`                | Enable processing of `X-Forwarded-For` and `X-Forwarded-Proto` headers. Always set to `true` when behind a reverse proxy or ingress. |

### CORS

CORS origins are configured via `appsettings.json`. When self-hosting, set `CorsOrigins` to include your public domain:

```json
{
  "CorsOrigins": ["https://your-domain.com"]
}
```

## Job server

The Job Server requires the same connection string, authentication, email, and S3 variables as the API server. The table below lists each one for reference.

| Variable                      | Description                                     |
| ----------------------------- | ----------------------------------------------- |
| ConnectionStrings\_\_netptune | PostgreSQL connection string (same as API).     |
| ConnectionStrings\_\_cache    | Redis / Valkey connection string (same as API). |
| ConnectionStrings\_\_nats     | NATS server URL (same as API).                  |
| NETPTUNE_SIGNING_KEY          | JWT signing key (same as API).                  |
| SEND_GRID_API_KEY             | SendGrid API key.                               |
| NETPTUNE_S3_BUCKET_NAME       | S3 bucket name.                                 |
| NETPTUNE_S3_REGION            | S3 region.                                      |
| NETPTUNE_S3_ACCESS_KEY_ID     | S3 access key ID.                               |
| NETPTUNE_S3_SECRET_ACCESS_KEY | S3 secret key.                                  |

## PostgreSQL

| Variable                  | Description                                           |
| ------------------------- | ----------------------------------------------------- |
| POSTGRES_USER             | Database superuser — use postgres.                    |
| POSTGRES_PASSWORD         | Superuser password. Use a strong random value.        |
| POSTGRES_DB               | Database name — must be netptune.                     |
| POSTGRES_HOST_AUTH_METHOD | Set to scram-sha-256 for encrypted authentication.    |
| POSTGRES_INITDB_ARGS      | \--auth-host=scram-sha-256 --auth-local=scram-sha-256 |

## Redis / Valkey

Start the server with a `--requirepass` flag:

```bash
valkey-server --requirepass <your_password>
```

Include the password in the connection string passed to the API and Job Server:

```
<password>@<hostname>:<port>
```

## NATS

NATS must run with JetStream enabled using the `-js` flag:

```bash
nats-server -js
```

:::note

No password is required for a basic self-hosted setup. For production environments, authentication can be added via NATS configuration files.

:::

## Client (Nginx)

The client container is a pre-built Angular application served by Nginx. No additional configuration is required. The following behaviours are baked in:

- All requests to `/api/*` are proxied to the API server
- All other routes fall back to the Angular SPA's index.html
- Static assets are served with a 1-year Cache-Control header
- Security headers: `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`
- Gzip compression enabled

## Helm values mapping

When deploying via Helm, environment variables are managed through `values.yaml` and `values.secret.yaml`. The table below maps Helm value paths to their corresponding environment variables.

| Helm path                          | Environment variable          |
| ---------------------------------- | ----------------------------- |
| secrets.api.signing_key            | NETPTUNE_SIGNING_KEY          |
| secrets.api.github_client_id       | NETPTUNE_GITHUB_CLIENT_ID     |
| secrets.api.github_secret          | NETPTUNE_GITHUB_SECRET        |
| secrets.api.github_callback        | NETPTUNE_GITHUB_CALLBACK      |
| secrets.api.google_client_id       | NETPTUNE_GOOGLE_CLIENT_ID     |
| secrets.api.google_secret          | NETPTUNE_GOOGLE_SECRET        |
| secrets.api.google_callback        | NETPTUNE_GOOGLE_CALLBACK      |
| secrets.api.microsoft_client_id    | NETPTUNE_MICROSOFT_CLIENT_ID  |
| secrets.api.microsoft_secret       | NETPTUNE_MICROSOFT_SECRET     |
| secrets.api.microsoft_callback     | NETPTUNE_MICROSOFT_CALLBACK   |
| secrets.api.sendgrid_api_key       | SEND_GRID_API_KEY             |
| secrets.api.s3_bucket_name         | NETPTUNE_S3_BUCKET_NAME       |
| secrets.api.s3_region              | NETPTUNE_S3_REGION            |
| secrets.api.s3_access_key_id       | NETPTUNE_S3_ACCESS_KEY_ID     |
| secrets.api.s3_secret_access_key   | NETPTUNE_S3_SECRET_ACCESS_KEY |
| secrets.postgres.postgres_password | POSTGRES_PASSWORD             |
| secrets.cache.cache_password       | Redis --requirepass value     |
