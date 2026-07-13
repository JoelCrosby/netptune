---
title: 'Configuration Reference'
description: 'Configuration currently read by the API, jobs, activity, client, and Helm chart.'
---

.NET configuration keys can be supplied as environment variables by replacing `:` with `__`. For example, `Tokens:Issuer` becomes `Tokens__Issuer`.

## Shared service connections

| Setting                          | Services            | Description                                                                                       |
| -------------------------------- | ------------------- | ------------------------------------------------------------------------------------------------- |
| `DATABASE_URL`                   | API, jobs, activity | Preferred PostgreSQL connection string.                                                           |
| `ConnectionStrings__netptune`    | API, jobs, activity | PostgreSQL fallback used when `DATABASE_URL` is absent.                                           |
| `REDIS_URL`                      | API, jobs, activity | Preferred Valkey/Redis connection string.                                                         |
| `ConnectionStrings__redis`       | API, jobs, activity | Valkey/Redis fallback used when `REDIS_URL` is absent.                                            |
| `ConnectionStrings__nats`        | API, jobs, activity | NATS connection URL. The server must have JetStream enabled.                                      |
| `ConnectionStrings__meilisearch` | API, jobs           | Aspire-style connection string, for example `Endpoint=http://meilisearch:7700/;MasterKey=secret`. |

The current Helm chart generates `REDIS_URL`, `ConnectionStrings__netptune`, `ConnectionStrings__nats`, and `ConnectionStrings__meilisearch` for the relevant workloads.

## API

### Authentication

| Variable                        | Required at startup | Description                                                                |
| ------------------------------- | ------------------- | -------------------------------------------------------------------------- |
| `NETPTUNE_SIGNING_KEY`          | Yes                 | Symmetric key used to sign authentication tokens. Use a long random value. |
| `NETPTUNE_GITHUB_CLIENT_ID`     | Yes                 | GitHub OAuth App client ID.                                                |
| `NETPTUNE_GITHUB_SECRET`        | Yes                 | GitHub OAuth App client secret.                                            |
| `NETPTUNE_GITHUB_CALLBACK`      | Yes                 | Local callback path handled by ASP.NET Core, such as `/signin-github`.     |
| `NETPTUNE_GOOGLE_CLIENT_ID`     | Yes                 | Google web OAuth client ID.                                                |
| `NETPTUNE_GOOGLE_SECRET`        | Yes                 | Google OAuth client secret.                                                |
| `NETPTUNE_GOOGLE_CALLBACK`      | Yes                 | Local callback path, such as `/signin-google`.                             |
| `NETPTUNE_MICROSOFT_CLIENT_ID`  | Yes                 | Microsoft identity application client ID.                                  |
| `NETPTUNE_MICROSOFT_SECRET`     | Yes                 | Microsoft application client secret.                                       |
| `NETPTUNE_MICROSOFT_CALLBACK`   | Yes                 | Local callback path, such as `/signin-microsoft`.                          |
| `NETPTUNE_TURNSTILE_SECRET_KEY` | Yes                 | Cloudflare Turnstile server-side secret used for login and registration.   |

The current API calls its required-environment-variable helper for all three OAuth providers. Blank values cause startup to fail; OAuth providers cannot currently be omitted through configuration alone.

### Origins, tokens, and email defaults

| Key                             | Image default                 | Description                                                                               |
| ------------------------------- | ----------------------------- | ----------------------------------------------------------------------------------------- |
| `Origin`                        | `https://app.netptune.co.uk/` | Public Angular client origin used when constructing links and redirects.                  |
| `CorsOrigins__0`                | `https://app.netptune.co.uk`  | First allowed credentialed CORS origin. Add further entries with `__1`, `__2`, and so on. |
| `Tokens__Issuer`                | `netptune.co.uk`              | Token issuer.                                                                             |
| `Tokens__Audience`              | `netptune.co.uk`              | Token audience.                                                                           |
| `Tokens__ExpireDays`            | `5`                           | Access-token lifetime in days.                                                            |
| `Email__DefaultFromAddress`     | `support@netptune.co.uk`      | Sender address passed to Cloudflare Email Sending.                                        |
| `Email__DefaultFromDisplayName` | `Netptune Support`            | Sender display name. The current Cloudflare payload uses the address value.               |

Override the origin and CORS values when deploying on another domain. `CorsOrigins` must contain the exact browser origin without a trailing slash.

### Cloudflare email

| Variable                          | Required | Description                                                      |
| --------------------------------- | -------- | ---------------------------------------------------------------- |
| `NETPTUNE_CLOUDFLARE_EMAIL_TOKEN` | Yes      | Cloudflare API token authorized to send email.                   |
| `NETPTUNE_CLOUDFLARE_ACCOUNT_ID`  | Yes      | Cloudflare account identifier used in the Email Sending API URL. |

The API publishes email work to NATS. The jobs service consumes that work and sends the rendered message through Cloudflare.

### S3 storage

| Variable                        | Required | Description                                     |
| ------------------------------- | -------- | ----------------------------------------------- |
| `NETPTUNE_S3_BUCKET_NAME`       | Yes      | Bucket used for attachments and audit archives. |
| `NETPTUNE_S3_REGION`            | Yes      | AWS region passed to the S3 client.             |
| `NETPTUNE_S3_ACCESS_KEY_ID`     | Yes      | S3 access key ID.                               |
| `NETPTUNE_S3_SECRET_ACCESS_KEY` | Yes      | S3 secret access key.                           |

The current storage options do not expose a custom S3 endpoint variable. AWS S3-style credentials and region are supported directly; arbitrary MinIO endpoints are not configurable by the current `Program.cs` files.

### Hosting

| Variable                              | Chart default                                | Description                                            |
| ------------------------------------- | -------------------------------------------- | ------------------------------------------------------ |
| `ASPNETCORE_URLS`                     | `http://0.0.0.0:7400`                        | API listen URL.                                        |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true`                                       | Enables forwarded-header integration in the container. |
| `HTTP_PORTS`                          | `7400`                                       | Port supplied by the chart.                            |
| `OTEL_EXPORTER_OTLP_ENDPOINT`         | Aspire service when enabled                  | OTLP collector endpoint.                               |
| `OTEL_EXPORTER_OTLP_HEADERS`          | Generated from `secrets.aspire.otlp_api_key` | Aspire Dashboard OTLP authentication header.           |

The API processes `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` and clears the default known-proxy restrictions.

## Jobs service

The jobs service requires PostgreSQL, Valkey, NATS, Meilisearch, Cloudflare email, and S3 configuration. It consumes these NATS subjects by default:

- `netptune.search`
- `netptune.email`
- `netptune.automation`

Automation scheduling can be adjusted with:

| Key                                  | Default    | Description                                 |
| ------------------------------------ | ---------- | ------------------------------------------- |
| `Automation__Schedule__StartupDelay` | `00:02:00` | Delay before scheduled automation begins.   |
| `Automation__Schedule__RunInterval`  | `01:00:00` | Interval between scheduled automation runs. |

## Activity service

The activity service requires PostgreSQL, Valkey, NATS, and S3 configuration. It consumes `netptune.activity` and supports:

| Key                                  | Default    | Description                            |
| ------------------------------------ | ---------- | -------------------------------------- |
| `Activity__Merge__WindowDuration`    | `00:05:00` | Normal activity merge window.          |
| `Activity__Merge__MaxWindowDuration` | `00:30:00` | Maximum merge window.                  |
| `Activity__Merge__SweepInterval`     | `00:00:30` | Frequency for closing expired windows. |

Running the activity image with `--job retention` performs one audit archive pass and exits. The chart uses this mode in a CronJob.

## Angular client

| Variable or build setting               | Description                                                                   |
| --------------------------------------- | ----------------------------------------------------------------------------- |
| `API_URL`                               | Runtime environment variable consumed by the Nginx template to proxy `/api/`. |
| `environment.prod.ts: turnstileSitekey` | Cloudflare Turnstile public site key compiled into the Angular bundle.        |

The chart maps `parameters.client.api_url` to `API_URL`. The Turnstile site key is not currently a runtime or Helm value, so changing it requires rebuilding the client image.

## Helm value mapping

| Helm value                           | Generated setting                                                             |
| ------------------------------------ | ----------------------------------------------------------------------------- |
| `secrets.postgres.postgres_password` | PostgreSQL server password                                                    |
| `secrets.cache.cache_password`       | Valkey server password                                                        |
| `secrets.meilisearch.master_key`     | `MEILI_MASTER_KEY` and application Meilisearch connection strings             |
| `secrets.api.*`                      | API database, cache, auth, email, Turnstile, and S3 settings                  |
| `secrets.jobs.*`                     | Jobs database, cache, email, and S3 settings                                  |
| `secrets.activity.*`                 | Activity database, cache, and S3 settings                                     |
| `config.api.*`                       | API ports, resource discovery values, NATS, and telemetry retry settings      |
| `config.jobs.*`                      | Jobs ports, resource discovery values, NATS, and telemetry retry settings     |
| `config.activity.*`                  | Activity ports, resource discovery values, NATS, and telemetry retry settings |

`sendgrid_api_key` and several `signing_key` fields remain in the chart schema but are not read by the current jobs or activity programs. Cloudflare Email Sending is the active email implementation.
