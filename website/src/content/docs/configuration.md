---
title: 'Configuration Reference'
description: 'Configuration currently read by the API, jobs, activity, client, and Helm chart.'
---

.NET configuration keys can be supplied as environment variables by replacing `:` with `__`. For example, `Tokens:Issuer` becomes `Tokens__Issuer`. The servers read configuration only this way; there are no separately named environment variables.

Required values are validated at startup. A missing one stops the process with an error naming the key.

## Shared service connections

| Setting                          | Services            | Description                                                                                       |
| -------------------------------- | ------------------- | ------------------------------------------------------------------------------------------------- |
| `ConnectionStrings__netptune`    | API, jobs, activity | PostgreSQL connection string.                                                                     |
| `ConnectionStrings__redis`       | API, jobs, activity | Valkey/Redis connection string.                                                                   |
| `ConnectionStrings__nats`        | API, jobs, activity | NATS connection URL. The server must have JetStream enabled.                                      |
| `ConnectionStrings__meilisearch` | API, jobs           | Aspire-style connection string, for example `Endpoint=http://meilisearch:7700/;MasterKey=secret`. |

The current Helm chart generates all four connection strings into the shared `server-secrets` Secret and `server-config` ConfigMap.

## API

### Authentication

| Key                                       | Required at startup | Description                                                                |
| ----------------------------------------- | ------------------- | -------------------------------------------------------------------------- |
| `Authentication__SigningKey`              | Yes                 | Symmetric key used to sign authentication tokens. Use a long random value. |
| `Authentication__GitHub__ClientId`        | Yes                 | GitHub OAuth App client ID.                                                |
| `Authentication__GitHub__ClientSecret`    | Yes                 | GitHub OAuth App client secret.                                            |
| `Authentication__GitHub__CallbackPath`    | Yes                 | Local callback path handled by ASP.NET Core, such as `/signin-github`.     |
| `Authentication__Google__ClientId`        | Yes                 | Google web OAuth client ID.                                                |
| `Authentication__Google__ClientSecret`    | Yes                 | Google OAuth client secret.                                                |
| `Authentication__Google__CallbackPath`    | Yes                 | Local callback path, such as `/signin-google`.                             |
| `Authentication__Microsoft__ClientId`     | Yes                 | Microsoft identity application client ID.                                  |
| `Authentication__Microsoft__ClientSecret` | Yes                 | Microsoft application client secret.                                       |
| `Authentication__Microsoft__CallbackPath` | Yes                 | Local callback path, such as `/signin-microsoft`.                          |
| `Turnstile__SecretKey`                    | Yes                 | Cloudflare Turnstile server-side secret used for login and registration.   |

The API validates all three OAuth provider configurations at startup. Blank values cause startup to fail; OAuth providers cannot currently be omitted through configuration alone.

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

| Key                            | Required | Description                                                      |
| ------------------------------ | -------- | ---------------------------------------------------------------- |
| `Email__Cloudflare__ApiToken`  | Yes      | Cloudflare API token authorized to send email.                   |
| `Email__Cloudflare__AccountId` | Yes      | Cloudflare account identifier used in the Email Sending API URL. |

The API publishes email work to NATS. The jobs service consumes that work and sends the rendered message through Cloudflare.

### S3 storage

| Key                            | Required | Description                                        |
| ------------------------------ | -------- | -------------------------------------------------- |
| `Storage__S3__BucketName`      | Yes      | Bucket used for uploaded media and audit archives. |
| `Storage__S3__Region`          | Yes      | AWS region passed to the S3 client.                |
| `Storage__S3__AccessKeyId`     | Yes      | S3 access key ID.                                  |
| `Storage__S3__SecretAccessKey` | Yes      | S3 secret access key.                              |

The current storage options do not expose a custom S3 endpoint key. AWS S3-style credentials and region are supported directly; arbitrary MinIO endpoints are not configurable by the current `Program.cs` files.

### AI assistant

The assistant runs on API keys supplied by users and workspace admins, so no provider key is configured here. These settings only shape how the harness behaves.

| Variable                      | Default         | Description                                                                                                            |
| ----------------------------- | --------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `Ai__AnthropicModel`          | `claude-opus-5` | Model used for conversations started on Anthropic.                                                                     |
| `Ai__OpenAiModel`             | `gpt-5.6-sol`   | Model used for conversations started on OpenAI.                                                                        |
| `Ai__GenerateTitles`          | `true`          | Names each new conversation with one extra call to a small model after the first reply.                                |
| `Ai__MaxToolIterations`       | `40`            | Rounds of tool calls allowed in one reply before the assistant stops and says so. 1–100.                               |
| `Ai__TurnTimeoutSeconds`      | `900`           | Time one reply may take before it stops and says so. 60–3600. The reply is still saved.                                |
| `Ai__ReviewMaxToolIterations` | `12`            | Rounds of tool calls one task review may make before it is stored as failed. 1–100.                                    |
| `Ai__ReviewTimeoutSeconds`    | `300`           | Time one task review may take before it is stored as failed. 60–3600.                                                  |
| `Ai__ReviewMaxTurnCharacters` | `120000`        | Tool result characters one task review may gather before it is stored as failed. Bounds what a review costs.           |
| `Ai__MaxOutputTokens`         | `16000`         | Output token ceiling per provider request.                                                                             |
| `Ai__MaxToolResultCharacters` | `32000`         | Tool results longer than this are truncated before the model sees them.                                                |
| `Ai__MaxTurnCharacters`       | `400000`        | Tool result characters one reply may gather before it stops and says so. Keeps a long reply inside the context window. |
| `Ai__MaxHistoryCharacters`    | `120000`        | Conversation replay budget. Older turns are dropped once a conversation exceeds it.                                    |
| `RateLimiting__AiPermitLimit` | `20`            | Assistant messages and change-set applies allowed per user per minute.                                                 |

Web research needs no server configuration. These settings only tune it.

| Variable                         | Default   | Description                                                              |
| -------------------------------- | --------- | ------------------------------------------------------------------------ |
| `Ai__Web__MaxSearchResults`      | `10`      | Upper bound on results per search, whatever the model asks for.          |
| `Ai__Web__TimeoutSeconds`        | `20`      | Per-request timeout for fetches and searches.                            |
| `Ai__Web__MaxResponseBytes`      | `5242880` | Bytes read from a response before the rest is discarded.                 |
| `Ai__Web__MaxDocumentCharacters` | `200000`  | Readable text kept per page after extraction.                            |
| `Ai__Web__MaxRedirects`          | `5`       | Redirect hops followed, each re-checked against the egress rules.        |
| `Ai__Web__DefaultPageCharacters` | `6000`    | Characters returned per read when the model does not ask for a size.     |
| `Ai__Web__MaxPageCharacters`     | `20000`   | Ceiling on a single `read_web_document` call.                            |
| `Ai__Web__RetentionHours`        | `24`      | How long a fetched page stays readable before the job server deletes it. |

The search provider is not configured here — it is per workspace. An admin picks one under workspace settings → Assistant → Web search, and the choice covers every member. Brave Search takes an API key; Google Programmable Search takes an API key and a search engine id (`cx`); SearXNG takes the base URL of a self-hosted instance and no key at all, because it has none — you will need `json` in that instance's `search.formats`. Keys are encrypted with the same ASP.NET Data Protection purpose as the assistant's provider keys, so the same keyring caveat applies: lose it and the key must be re-entered. With no provider set up, `web_search` tells the model to ask an admin, while `web_fetch` keeps working on links it is given.

Only the fetcher enforces the egress rules below. A search endpoint is set by an admin rather than chosen by the model, so a SearXNG instance on a private cluster address is reachable for search while `web_fetch` on that same address stays blocked.

A fetched page is never returned whole. `web_fetch` strips scripts, navigation and other chrome, stores the readable text in `ai_web_documents`, and returns the opening few thousand characters with a `documentId`; the model pages through the rest with `read_web_document`. That keeps a research turn inside the same tool-result and history budgets as any other tool, instead of one long article filling the window. The stored rows are disposable — they expire after `Ai__Web__RetentionHours`, the job server sweeps them hourly, and an expired id simply tells the model to fetch again. They are excluded from workspace exports.

Only public hosts are reachable. Fetches are limited to `http` and `https`, URLs carrying credentials are refused, and loopback, private, link-local, carrier-grade NAT and cloud metadata addresses are blocked — checked when the host resolves, again on every redirect hop, and once more on the socket itself so a DNS answer that changes in between cannot reach an internal service. Fetching is gated on `assistant.use_web`, granted to Member and above but not to Viewers, and never to anonymous visitors of a public workspace. That permission and the workspace assistant switch are the only gates — there is no deployment-level off switch, so a deployment that must not reach the internet at all should block it at the network, or revoke `assistant.use_web` from every role.

Page content is data, not instruction. The tool description says so, and because every write tool only ever proposes a change for a member to review and apply, a page that tells the assistant to modify the workspace still cannot do it on its own.

The two model settings are only fallbacks. The models users can actually pick come from a fixed catalogue served by `GET /api/ai/models` and defined in `AiModels.Catalog` (`server/Netptune.Core/Models/Ai/AiModels.cs`) — edit that list to offer different models. Users choose one per API key in personal settings, and can switch models mid-conversation from the assistant panel; switching to a model from the other provider moves the conversation to that provider, which needs a key for it. A conversation otherwise keeps the model it started with, so changing these settings affects new conversations only.

Conversation titles are written by the model. After the first reply the assistant makes one short extra call — on the cheapest catalogue model for that provider, not the conversation's own model — and names the conversation from it. Its tokens are counted against that first assistant message, so the usage totals include it. If the call fails the title stays as the truncated first message, and `Ai__GenerateTitles=false` skips it entirely.

Users supply their own provider keys from personal settings, and a workspace admin can add a shared key per provider from workspace settings so members can use the assistant without adding one of their own. A member's own key always wins over the workspace key, so anyone who has added one keeps their own account and quota. Keys are encrypted with ASP.NET Data Protection under the purpose `netptune.ai-credentials`, so the data-protection keyring in Valkey/Redis must be stable — losing it makes stored keys unreadable and they must be re-entered.

Reaching the assistant at all is gated on `assistant.use`, granted to Member and above but not to Viewers, and never to anonymous visitors of a public workspace. Revoking it from a member hides the assistant everywhere in the client and refuses every conversation, change set and personal key endpoint under `/api/ai`. Workspace admins can turn the assistant off for a whole workspace from workspace settings, and the `assistant.read_all_conversations` permission (granted to Admin and Owner) exposes every member's conversations there. Deleting any member's conversation from that page needs `assistant.delete_any_conversations`, also granted to Admin and Owner; each deletion is written to the audit log. Managing the shared workspace key needs `workspace.update`.

A proposed change carries its values as ids and display text rather than one rendered string, so the review table draws avatars, status pills and dates in the reviewer's own format instead of parsing them back out of a sentence. Tools build these with `AiChangeFields` (`server/Netptune.Core/Services/Ai/AiChangeFields.cs`), which derives the rendered text from the values so the two cannot disagree, and both sides of the JSON column go through `AiChangeFieldSerializer`. A field left as plain text still works — and a change set proposed before this, still sitting unapplied, reviews exactly as it did.

What the assistant can reach is decided by the member's own permissions, not by configuration. Every tool declares the permissions it needs (`server/Netptune.Ai/Tools/`) and the runner offers a member only the tools they already hold — so a member without `automations.read` is never told the automation tools exist. A tool that covers several kinds of change, such as `propose_update_task` for fields, assignees and tags, is offered when the member can make any of them, and each call is checked against the permissions its arguments need. Applying a change checks the member's permissions again. Reads cover projects, tasks, sprints, boards, comments, relations, tags, statuses, members, files, automation rules and runs, and the reporting metrics (flow, workload, sprint burndown and velocity). Writes are always proposals: a `propose_` tool adds an entry to a change set the member reviews and applies, and nothing reaches the database until they do. File contents are never read — the file tools report what is attached, not what is inside.

Token usage is priced from a table of published per-model rates in `AiModelPricing` (`server/Netptune.Core/Models/Ai/AiModelPricing.cs`), which charges input, output, cached-read and cached-write tokens at their own rates. Every catalogue model needs an entry — a model without one reports no cost rather than an error, so add its rates alongside the catalogue entry. The figure is an estimate in US dollars: a conversation is priced at the model it currently uses, so a conversation that switched models, and the cheaper model used to write its title, are both priced at that one rate. Members see the running total for the open chat above the message box, and admins see the workspace total on the assistant conversations page. Nothing bills through Netptune — the charge lands on whichever API key answered the request.

The assistant's tables are created with the rest of the schema when the API first starts, so there is nothing to run by hand. An existing database predating the shared workspace key needs `server/scripts/add-workspace-ai-credentials.sql`, and one predating change-set undo needs `server/scripts/add-ai-change-set-undo.sql`.

### Hosting

| Variable                              | Chart default                                | Description                                            |
| ------------------------------------- | -------------------------------------------- | ------------------------------------------------------ |
| `ASPNETCORE_URLS`                     | `http://0.0.0.0:7400`                        | API listen URL.                                        |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true`                                       | Enables forwarded-header integration in the container. |
| `HTTP_PORTS`                          | `7400`                                       | Port supplied by the chart.                            |
| `OTEL_EXPORTER_OTLP_ENDPOINT`         | Aspire service when enabled                  | OTLP collector endpoint.                               |
| `OTEL_EXPORTER_OTLP_HEADERS`          | Generated from `secrets.aspire.otlp_api_key` | Aspire Dashboard OTLP authentication header.           |

The API processes `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` and clears the default known-proxy restrictions.

The API uses the same PostgreSQL, Valkey, NATS, forwarded-header, and OTLP settings, from the shared `server-config` and `server-secrets` plus its own `config.api`. Its chart port is `7600`; it does not require JWT signing or third-party OAuth secrets because it accepts API credentials only.

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

| Helm value                           | Generated setting                                                                                                                                                       |
| ------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `secrets.postgres.postgres_password` | PostgreSQL server password and `ConnectionStrings__netptune` in `server-secrets`                                                                                        |
| `secrets.cache.cache_password`       | Valkey server password and `ConnectionStrings__redis` in `server-secrets`                                                                                               |
| `secrets.meilisearch.master_key`     | `MEILI_MASTER_KEY` and `ConnectionStrings__meilisearch` in `server-secrets`                                                                                             |
| `secrets.aspire.otlp_api_key`        | `OTEL_EXPORTER_OTLP_HEADERS` in `server-secrets` when `aspire.enabled`                                                                                                  |
| `serverSecrets.*`                    | Rendered verbatim into the shared `server-secrets` Secret: auth, email, Turnstile, and S3 secrets                                                                       |
| `serverConfig.*`                     | Rendered verbatim into the shared `server-config` ConfigMap: OAuth client IDs and callback paths                                                                        |
| `trustedProxies.*`                   | `TrustedProxies__*` settings; `TrustedProxies__EdgeAuthorizationHeader` is added to `server-config` when `serverSecrets.TrustedProxies__EdgeAuthorizationSecret` is set |
| `config.app.*`, `config.api.*`       | Per-component ports, resource discovery values, and telemetry retry settings                                                                                            |
| `config.jobs.*`                      | Jobs ports, resource discovery values, and telemetry retry settings                                                                                                     |
| `config.activity.*`                  | Activity ports, resource discovery values, and telemetry retry settings                                                                                                 |

`server-secrets` and `server-config` are mounted into the app, API, jobs, and activity pods and the activity retention CronJob. Their keys are the env-form configuration keys on this page, so supplying a new value needs no template change:

```yaml
serverSecrets:
  Storage__S3__SecretAccessKey: change-me
serverConfig:
  Authentication__GitHub__CallbackPath: /signin-github
```

`charts/netptune/values.secret.yaml.example` shows the full shape. Cloudflare Email Sending is the active email implementation.
