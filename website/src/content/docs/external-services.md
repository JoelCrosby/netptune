---
title: 'External Services'
description: 'Set up SendGrid, GitHub OAuth, and S3-compatible storage before deploying.'
---

Netptune depends on a few external services for email delivery, social authentication, and file storage. This page walks through setting each one up from scratch.

## SendGrid

Netptune uses SendGrid to send transactional emails such as workspace invitations and notifications. A free tier account supports up to 100 emails per day, which is sufficient for small teams.

1. **Create a SendGrid account**

Sign up at [sendgrid.com](https://sendgrid.com).

2. **Verify a sender identity**

Before SendGrid will send emails on your behalf, you must verify either a single sender address or an entire domain.

- Go to Settings → Sender Authentication
- Choose Single Sender Verification for a quick setup
- Or choose Domain Authentication for production use (recommended)
- Follow the instructions to verify your address or add the required DNS records

3. **Create an API key**

- Go to Settings → API Keys → Create API Key
- Select Restricted Access and enable Mail Send → Full Access
- Copy the generated key — it is only shown once

Set the following environment variables:

| Variable                    | Value                             |
| --------------------------- | --------------------------------- |
| SEND_GRID_API_KEY           | The API key you just generated    |
| Email\_\_DefaultFromAddress | The verified sender email address |

## GitHub OAuth App

GitHub login is optional. If you skip this, users can still register with an email address and password.

1. **Create an OAuth App**

- Go to [github.com/settings/developers](https://github.com/settings/developers)
- Click New OAuth App

| Field                      | Value                                              |
| -------------------------- | -------------------------------------------------- |
| Application name           | Netptune (or any name you prefer)                  |
| Homepage URL               | `https://your-domain.com`                          |
| Authorization callback URL | `https://your-domain.com/api/auth/github/callback` |

Click Register application.

2. **Generate a client secret**

On the app detail page, click Generate a new client secret and copy it immediately.

3. **Set the environment variables**

| Variable                  | Value                                  |
| ------------------------- | -------------------------------------- |
| NETPTUNE_GITHUB_CLIENT_ID | The Client ID from the app detail page |
| NETPTUNE_GITHUB_SECRET    | The client secret you just generated   |

## S3-compatible storage

Netptune stores file attachments in an S3-compatible bucket. You can use AWS S3 or a self-hosted alternative such as MinIO.

### Option A — AWS S3

1. **Create a bucket**

- Go to the S3 console and click Create bucket
- Choose a region (e.g. `us-east-1`)
- Keep Block all public access enabled — Netptune uses signed URLs for access

2. **Create an IAM user**

Create an IAM user with programmatic access and attach a policy scoped to your bucket:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject", "s3:ListBucket"],
      "Resource": ["arn:aws:s3:::your-bucket-name", "arn:aws:s3:::your-bucket-name/*"]
    }
  ]
}
```

Save the Access Key ID and Secret Access Key.

3. **Set the environment variables**

| Variable                      | Value                 |
| ----------------------------- | --------------------- |
| NETPTUNE_S3_BUCKET_NAME       | Your bucket name      |
| NETPTUNE_S3_REGION            | e.g. us-east-1        |
| NETPTUNE_S3_ACCESS_KEY_ID     | IAM access key ID     |
| NETPTUNE_S3_SECRET_ACCESS_KEY | IAM secret access key |

### Option B — MinIO (self-hosted)

MinIO is a self-hosted, S3-compatible object store that can run as a Docker container alongside Netptune.

1. **Add MinIO to your Compose file**

```yaml
minio:
  image: quay.io/minio/minio:latest
  restart: unless-stopped
  command: server /data --console-address ":9001"
  environment:
    MINIO_ROOT_USER: '${MINIO_ACCESS_KEY}'
    MINIO_ROOT_PASSWORD: '${MINIO_SECRET_KEY}'
  volumes:
    - minio_data:/data
  ports:
    - '9000:9000' # S3 API
    - '9001:9001' # MinIO web console

volumes:
  minio_data:
```

Add your MinIO credentials to your `.env`:

```env
MINIO_ACCESS_KEY=change_me_minio_user
MINIO_SECRET_KEY=change_me_minio_password
```

2. **Create the bucket**

Access the MinIO web console at `http://your-server:9001`, log in, and create a bucket named `netptune`.

3. **Set the environment variables**

| Variable                      | Value                                 |
| ----------------------------- | ------------------------------------- |
| NETPTUNE_S3_BUCKET_NAME       | netptune                              |
| NETPTUNE_S3_REGION            | us-east-1 (any value works for MinIO) |
| NETPTUNE_S3_ACCESS_KEY_ID     | Value of MINIO_ACCESS_KEY             |
| NETPTUNE_S3_SECRET_ACCESS_KEY | Value of MINIO_SECRET_KEY             |

:::note

If MinIO runs on a custom hostname or port, you may also need to set `NETPTUNE_S3_SERVICE_URL` to point at `http://minio:9000`. Check the API server logs if uploads fail.

:::

## Summary checklist

Before starting Netptune, confirm you have all of the following:

- SendGrid API key
- Verified sender email address in SendGrid
- S3 bucket created (AWS or MinIO)
- S3 access key ID and secret key
- GitHub OAuth App client ID and secret (optional)
- All values populated in your .env or Helm secrets file
