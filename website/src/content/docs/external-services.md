---
title: 'External Services'
description: 'Configure the external services required by the current Netptune server.'
---

The current API expects S3 storage, Cloudflare Email Sending, Cloudflare Turnstile, and credentials for GitHub, Google, and Microsoft authentication.

## Cloudflare Email Sending

Netptune sends transactional email through Cloudflare's Email Sending API. Both the API and jobs service require the same Cloudflare account credentials.

1. Enable Email Sending for the Cloudflare account that owns your sender domain.
2. Verify and configure the sender address used by `Email__DefaultFromAddress`.
3. Create an API token authorized to send email.
4. Copy the Cloudflare account ID.

| Setting                           | Value                   |
| --------------------------------- | ----------------------- |
| `NETPTUNE_CLOUDFLARE_EMAIL_TOKEN` | Cloudflare API token    |
| `NETPTUNE_CLOUDFLARE_ACCOUNT_ID`  | Cloudflare account ID   |
| `Email__DefaultFromAddress`       | Verified sender address |
| `Email__DefaultFromDisplayName`   | Sender display name     |

The jobs service calls `POST /client/v4/accounts/{account_id}/email/sending/send`. See the [Cloudflare Email Sending API](https://developers.cloudflare.com/api/resources/email_sending/methods/send/).

## Cloudflare Turnstile

Turnstile is enforced on email/password login and registration.

1. In Cloudflare, create a Turnstile widget for the Angular application's hostname.
2. Copy its site key and secret key.
3. Set `NETPTUNE_TURNSTILE_SECRET_KEY` on the API.
4. Rebuild the Angular client with the site key in `client/src/environments/environment.prod.ts`.

:::warning

Only the secret key is exposed through the Helm chart. The public site key is currently compiled into the client bundle and defaults to the Netptune-hosted widget, so using a different hostname requires a custom client build.

:::

Cloudflare documents widget creation and server-side verification in its [Turnstile getting-started guide](https://developers.cloudflare.com/turnstile/get-started/).

## OAuth callback model

Netptune uses a local ASP.NET Core callback path for each provider. The provider's registered redirect URI is the Angular application's public origin plus that path.

For an application at `https://app.example.com`, a typical configuration is:

| Provider  | Server callback setting                         | Provider redirect URI                      |
| --------- | ----------------------------------------------- | ------------------------------------------ |
| GitHub    | `NETPTUNE_GITHUB_CALLBACK=/signin-github`       | `https://app.example.com/signin-github`    |
| Google    | `NETPTUNE_GOOGLE_CALLBACK=/signin-google`       | `https://app.example.com/signin-google`    |
| Microsoft | `NETPTUNE_MICROSOFT_CALLBACK=/signin-microsoft` | `https://app.example.com/signin-microsoft` |

Callback settings must be paths beginning with `/`, while the provider consoles require complete HTTPS URLs.

## GitHub OAuth App

Create an OAuth App from GitHub's **Settings → Developer settings → OAuth Apps** page.

| Field                      | Example                                 |
| -------------------------- | --------------------------------------- |
| Application name           | Netptune                                |
| Homepage URL               | `https://app.example.com`               |
| Authorization callback URL | `https://app.example.com/signin-github` |

After registering it, generate a client secret and configure:

```env
NETPTUNE_GITHUB_CLIENT_ID=your-client-id
NETPTUNE_GITHUB_SECRET=your-client-secret
NETPTUNE_GITHUB_CALLBACK=/signin-github
```

See GitHub's [OAuth App creation guide](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/creating-an-oauth-app).

## Google OAuth client

In Google Cloud:

1. Configure the OAuth consent screen.
2. Create an OAuth client with application type **Web application**.
3. Add `https://app.example.com/signin-google` as an authorized redirect URI.
4. Copy the client ID and client secret.

```env
NETPTUNE_GOOGLE_CLIENT_ID=your-client-id
NETPTUNE_GOOGLE_SECRET=your-client-secret
NETPTUNE_GOOGLE_CALLBACK=/signin-google
```

Google requires the redirect URI to match exactly. See its [web-server OAuth guide](https://developers.google.com/identity/protocols/oauth2/web-server).

## Microsoft identity application

In the Microsoft Entra admin center:

1. Register an application.
2. Add a **Web** redirect URI of `https://app.example.com/signin-microsoft`.
3. Create a client secret.
4. Copy the application (client) ID and secret value.

```env
NETPTUNE_MICROSOFT_CLIENT_ID=your-client-id
NETPTUNE_MICROSOFT_SECRET=your-client-secret
NETPTUNE_MICROSOFT_CALLBACK=/signin-microsoft
```

:::note

The current API reads all three provider configurations with a required-value helper during startup. Supply non-empty values for GitHub, Google, and Microsoft even if you only intend to advertise one provider.

:::

## S3 storage

Netptune uses S3 for uploaded profile pictures and rich-text media. The activity-retention job also writes audit archives to the same configured bucket before deleting archived database rows.

Create a private bucket and an access key with permission to list the bucket and read, write, and delete its objects. Configure the same values on the API, jobs, and activity services:

```env
NETPTUNE_S3_BUCKET_NAME=netptune
NETPTUNE_S3_REGION=us-east-1
NETPTUNE_S3_ACCESS_KEY_ID=your-access-key
NETPTUNE_S3_SECRET_ACCESS_KEY=your-secret-key
```

A minimal AWS IAM policy scoped to one bucket is:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": ["arn:aws:s3:::netptune"]
    },
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": ["arn:aws:s3:::netptune/*"]
    }
  ]
}
```

:::warning

The current service wiring does not read a custom S3 endpoint or service URL. Although the storage library uses the S3 API, a self-hosted MinIO endpoint cannot currently be selected through an environment variable.

:::

## Readiness checklist

- Cloudflare Email Sending enabled with an API token and account ID
- Verified sender address configured in the API and jobs service
- Turnstile widget created for the application hostname
- Turnstile site key compiled into the client and secret configured on the API
- GitHub, Google, and Microsoft OAuth redirect URIs registered
- Private S3 bucket and credentials configured on API, jobs, and activity
- Application origin and CORS settings updated for the public hostname
