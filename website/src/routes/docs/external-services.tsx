import { For } from 'solid-js';
import Callout from '~/components/docs/Callout';
import CodeBlock from '~/components/docs/CodeBlock';
import DocLayout from '~/components/docs/DocLayout';
import DocPagination from '~/components/docs/DocPagination';
import DocTable from '~/components/docs/DocTable';

const h2 = 'mt-12 mb-4 text-xl font-semibold text-slate-900 dark:text-white';
const h3 = 'mt-8 mb-3 text-base font-semibold text-slate-900 dark:text-white';
const p = 'mb-4 leading-7 text-slate-600 dark:text-white/65';
const step = 'mb-2 flex items-center gap-3 text-sm font-semibold text-slate-900 dark:text-white';
const stepNum =
  'flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-slate-200 text-xs font-bold text-slate-600 dark:bg-white/15 dark:text-white/70';
const th =
  'px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-white/40';
const td = 'px-4 py-3 text-slate-600 dark:text-white/65 align-top';
const tdFirst = 'px-4 py-3 font-medium text-slate-800 dark:text-white/85 align-top';
const tdMono = 'px-4 py-3 font-mono text-[13px] text-slate-700 dark:text-white/75 align-top';
const tr = 'border-b border-slate-100 last:border-0 dark:border-white/5';
const thead = 'border-b border-slate-200 bg-slate-50 dark:border-white/10 dark:bg-white/5';
const mono = 'font-mono text-[13px]';

export default function ExternalServicesPage() {
  return (
    <DocLayout
      title="External Services"
      description="Set up SendGrid, GitHub OAuth, and S3-compatible storage before deploying."
    >
      <p class={p}>
        Netptune depends on a few external services for email delivery, social authentication, and
        file storage. This page walks through setting each one up from scratch.
      </p>

      {/* ── SendGrid ── */}
      <h2 class={h2}>SendGrid</h2>
      <p class={p}>
        Netptune uses SendGrid to send transactional emails such as workspace invitations and
        notifications. A free tier account supports up to 100 emails per day, which is sufficient
        for small teams.
      </p>

      <div class="mt-6 space-y-6">
        <div>
          <p class={step}>
            <span class={stepNum}>1</span>
            Create a SendGrid account
          </p>
          <p class={p}>
            Sign up at{' '}
            <a
              href="https://sendgrid.com"
              target="_blank"
              rel="noopener noreferrer"
              class="text-brand underline underline-offset-2 hover:text-brand-dark"
            >
              sendgrid.com
            </a>
            .
          </p>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>2</span>
            Verify a sender identity
          </p>
          <p class={p}>
            Before SendGrid will send emails on your behalf, you must verify either a single sender
            address or an entire domain.
          </p>
          <ul class="mb-4 space-y-1.5 text-sm text-slate-600 dark:text-white/65">
            <li>Go to Settings → Sender Authentication</li>
            <li>Choose Single Sender Verification for a quick setup</li>
            <li>Or choose Domain Authentication for production use (recommended)</li>
            <li>Follow the instructions to verify your address or add the required DNS records</li>
          </ul>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>3</span>
            Create an API key
          </p>
          <ul class="mb-4 space-y-1.5 text-sm text-slate-600 dark:text-white/65">
            <li>Go to Settings → API Keys → Create API Key</li>
            <li>Select Restricted Access and enable Mail Send → Full Access</li>
            <li>Copy the generated key — it is only shown once</li>
          </ul>
          <p class={p}>Set the following environment variables:</p>
          <DocTable>
            <thead class={thead}>
              <tr>
                <th class={th}>Variable</th>
                <th class={th}>Value</th>
              </tr>
            </thead>
            <tbody>
              <tr class={tr}>
                <td class={tdMono}>SEND_GRID_API_KEY</td>
                <td class={td}>The API key you just generated</td>
              </tr>
              <tr class={tr}>
                <td class={tdMono}>Email__DefaultFromAddress</td>
                <td class={td}>The verified sender email address</td>
              </tr>
            </tbody>
          </DocTable>
        </div>
      </div>

      {/* ── GitHub OAuth ── */}
      <h2 class={h2}>GitHub OAuth App</h2>
      <p class={p}>
        GitHub login is optional. If you skip this, users can still register with an email address
        and password.
      </p>

      <div class="mt-6 space-y-6">
        <div>
          <p class={step}>
            <span class={stepNum}>1</span>
            Create an OAuth App
          </p>
          <ul class="mb-4 space-y-1.5 text-sm text-slate-600 dark:text-white/65">
            <li>
              Go to{' '}
              <a
                href="https://github.com/settings/developers"
                target="_blank"
                rel="noopener noreferrer"
                class="text-brand underline underline-offset-2 hover:text-brand-dark"
              >
                github.com/settings/developers
              </a>
            </li>
            <li>Click New OAuth App</li>
          </ul>
          <DocTable>
            <thead class={thead}>
              <tr>
                <th class={th}>Field</th>
                <th class={th}>Value</th>
              </tr>
            </thead>
            <tbody>
              <tr class={tr}>
                <td class={tdFirst}>Application name</td>
                <td class={td}>Netptune (or any name you prefer)</td>
              </tr>
              <tr class={tr}>
                <td class={tdFirst}>Homepage URL</td>
                <td class={td}>
                  <code class={mono}>https://your-domain.com</code>
                </td>
              </tr>
              <tr class={tr}>
                <td class={tdFirst}>Authorization callback URL</td>
                <td class={td}>
                  <code class={mono}>https://your-domain.com/api/auth/github/callback</code>
                </td>
              </tr>
            </tbody>
          </DocTable>
          <p class={p}>Click Register application.</p>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>2</span>
            Generate a client secret
          </p>
          <p class={p}>
            On the app detail page, click Generate a new client secret and copy it immediately.
          </p>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>3</span>
            Set the environment variables
          </p>
          <DocTable>
            <thead class={thead}>
              <tr>
                <th class={th}>Variable</th>
                <th class={th}>Value</th>
              </tr>
            </thead>
            <tbody>
              <tr class={tr}>
                <td class={tdMono}>NETPTUNE_GITHUB_CLIENT_ID</td>
                <td class={td}>The Client ID from the app detail page</td>
              </tr>
              <tr class={tr}>
                <td class={tdMono}>NETPTUNE_GITHUB_SECRET</td>
                <td class={td}>The client secret you just generated</td>
              </tr>
            </tbody>
          </DocTable>
        </div>
      </div>

      {/* ── S3 ── */}
      <h2 class={h2}>S3-compatible storage</h2>
      <p class={p}>
        Netptune stores file attachments in an S3-compatible bucket. You can use AWS S3 or a
        self-hosted alternative such as MinIO.
      </p>

      <h3 class={h3}>Option A — AWS S3</h3>

      <div class="mt-4 space-y-6">
        <div>
          <p class={step}>
            <span class={stepNum}>1</span>
            Create a bucket
          </p>
          <ul class="mb-4 space-y-1.5 text-sm text-slate-600 dark:text-white/65">
            <li>Go to the S3 console and click Create bucket</li>
            <li>
              Choose a region (e.g. <code class={mono}>us-east-1</code>)
            </li>
            <li>Keep Block all public access enabled — Netptune uses signed URLs for access</li>
          </ul>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>2</span>
            Create an IAM user
          </p>
          <p class={p}>
            Create an IAM user with programmatic access and attach a policy scoped to your bucket:
          </p>
          <CodeBlock language="json">{`{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::your-bucket-name",
        "arn:aws:s3:::your-bucket-name/*"
      ]
    }
  ]
}`}</CodeBlock>
          <p class={p}>Save the Access Key ID and Secret Access Key.</p>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>3</span>
            Set the environment variables
          </p>
          <DocTable>
            <thead class={thead}>
              <tr>
                <th class={th}>Variable</th>
                <th class={th}>Value</th>
              </tr>
            </thead>
            <tbody>
              <For
                each={[
                  ['NETPTUNE_S3_BUCKET_NAME', 'Your bucket name'],
                  ['NETPTUNE_S3_REGION', 'e.g. us-east-1'],
                  ['NETPTUNE_S3_ACCESS_KEY_ID', 'IAM access key ID'],
                  ['NETPTUNE_S3_SECRET_ACCESS_KEY', 'IAM secret access key'],
                ]}
              >
                {([name, desc]) => (
                  <tr class={tr}>
                    <td class={tdMono}>{name}</td>
                    <td class={td}>{desc}</td>
                  </tr>
                )}
              </For>
            </tbody>
          </DocTable>
        </div>
      </div>

      <h3 class={h3}>Option B — MinIO (self-hosted)</h3>
      <p class={p}>
        MinIO is a self-hosted, S3-compatible object store that can run as a Docker container
        alongside Netptune.
      </p>

      <div class="mt-4 space-y-6">
        <div>
          <p class={step}>
            <span class={stepNum}>1</span>
            Add MinIO to your Compose file
          </p>
          <CodeBlock language="yaml">{`minio:
  image: quay.io/minio/minio:latest
  restart: unless-stopped
  command: server /data --console-address ":9001"
  environment:
    MINIO_ROOT_USER: "\${MINIO_ACCESS_KEY}"
    MINIO_ROOT_PASSWORD: "\${MINIO_SECRET_KEY}"
  volumes:
    - minio_data:/data
  ports:
    - "9000:9000"   # S3 API
    - "9001:9001"   # MinIO web console

volumes:
  minio_data:`}</CodeBlock>
          <p class={p}>
            Add your MinIO credentials to your <code class={mono}>.env</code>:
          </p>
          <CodeBlock language="env">{`MINIO_ACCESS_KEY=change_me_minio_user
MINIO_SECRET_KEY=change_me_minio_password`}</CodeBlock>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>2</span>
            Create the bucket
          </p>
          <p class={p}>
            Access the MinIO web console at <code class={mono}>http://your-server:9001</code>, log
            in, and create a bucket named <code class={mono}>netptune</code>.
          </p>
        </div>

        <div>
          <p class={step}>
            <span class={stepNum}>3</span>
            Set the environment variables
          </p>
          <DocTable>
            <thead class={thead}>
              <tr>
                <th class={th}>Variable</th>
                <th class={th}>Value</th>
              </tr>
            </thead>
            <tbody>
              <For
                each={[
                  ['NETPTUNE_S3_BUCKET_NAME', 'netptune'],
                  ['NETPTUNE_S3_REGION', 'us-east-1 (any value works for MinIO)'],
                  ['NETPTUNE_S3_ACCESS_KEY_ID', 'Value of MINIO_ACCESS_KEY'],
                  ['NETPTUNE_S3_SECRET_ACCESS_KEY', 'Value of MINIO_SECRET_KEY'],
                ]}
              >
                {([name, desc]) => (
                  <tr class={tr}>
                    <td class={tdMono}>{name}</td>
                    <td class={td}>{desc}</td>
                  </tr>
                )}
              </For>
            </tbody>
          </DocTable>
          <Callout type="note">
            If MinIO runs on a custom hostname or port, you may also need to set{' '}
            <code class={mono}>NETPTUNE_S3_SERVICE_URL</code> to point at{' '}
            <code class={mono}>http://minio:9000</code>. Check the API server logs if uploads fail.
          </Callout>
        </div>
      </div>

      {/* ── Checklist ── */}
      <h2 class={h2}>Summary checklist</h2>
      <p class={p}>Before starting Netptune, confirm you have all of the following:</p>
      <ul class="mb-6 space-y-2.5">
        <For
          each={[
            'SendGrid API key',
            'Verified sender email address in SendGrid',
            'S3 bucket created (AWS or MinIO)',
            'S3 access key ID and secret key',
            'GitHub OAuth App client ID and secret (optional)',
            'All values populated in your .env or Helm secrets file',
          ]}
        >
          {(item) => (
            <li class="flex items-start gap-2.5 text-sm text-slate-600 dark:text-white/65">
              <span class="mt-1 flex h-4 w-4 shrink-0 items-center justify-center rounded border border-slate-300 dark:border-white/20" />
              {item}
            </li>
          )}
        </For>
      </ul>

      <DocPagination prev={{ href: '/docs/configuration', label: 'Configuration' }} />
    </DocLayout>
  );
}
