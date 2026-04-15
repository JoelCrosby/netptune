import { For } from 'solid-js';
import Callout from '~/components/docs/Callout';
import CodeBlock from '~/components/docs/CodeBlock';
import DocLayout from '~/components/docs/DocLayout';
import DocPagination from '~/components/docs/DocPagination';
import DocTable from '~/components/docs/DocTable';

const h2 = 'mt-12 mb-4 text-xl font-semibold text-slate-900 dark:text-white';
const h3 = 'mt-8 mb-3 text-base font-semibold text-slate-900 dark:text-white';
const p = 'mb-4 leading-7 text-slate-600 dark:text-white/65';
const th =
  'px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-white/40';
const td = 'px-4 py-3 text-slate-600 dark:text-white/65 align-top';
const tdFirst = 'px-4 py-3 font-medium text-slate-800 dark:text-white/85 align-top';
const tdMono = 'px-4 py-3 font-mono text-[13px] text-slate-700 dark:text-white/75 align-top';
const tr = 'border-b border-slate-100 last:border-0 dark:border-white/5';
const thead = 'border-b border-slate-200 bg-slate-50 dark:border-white/10 dark:bg-white/5';
const mono = 'font-mono text-[13px]';

export default function ConfigurationPage() {
  return (
    <DocLayout
      title="Configuration Reference"
      description="All environment variables and settings for every Netptune service."
    >
      <p class={p}>
        Netptune services are configured entirely through environment variables. The sections below
        document every option for each service. For Helm deployments, see the mapping table at the
        bottom of this page.
      </p>

      {/* ── API Server ── */}
      <h2 class={h2}>API server</h2>

      <h3 class={h3}>Connection strings</h3>
      <p class={p}>
        Passed as environment variables using ASP.NET Core's double-underscore (
        <code class={mono}>__</code>) separator for nested configuration keys.
      </p>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Example</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdMono}>ConnectionStrings__netptune</td>
            <td class={td}>
              <code class={mono}>
                Host=postgres;Port=5432;Username=postgres;Password=secret;Database=netptune
              </code>
            </td>
            <td class={td}>
              PostgreSQL connection string. The database must exist and be accessible.
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>ConnectionStrings__cache</td>
            <td class={td}>
              <code class={mono}>password@cache:6379</code>
            </td>
            <td class={td}>Redis / Valkey connection string.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>ConnectionStrings__nats</td>
            <td class={td}>
              <code class={mono}>nats://nats:4222</code>
            </td>
            <td class={td}>NATS server URL. JetStream must be enabled on the server.</td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>Authentication</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Required</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_SIGNING_KEY</td>
            <td class={td}>Yes</td>
            <td class={td}>
              Secret key used to sign JWT tokens. Must be a long random string. Generate with{' '}
              <code class={mono}>openssl rand -base64 64</code>.
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_GITHUB_CLIENT_ID</td>
            <td class={td}>No</td>
            <td class={td}>GitHub OAuth App client ID. Leave blank to disable GitHub login.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_GITHUB_SECRET</td>
            <td class={td}>No</td>
            <td class={td}>GitHub OAuth App client secret.</td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>Token behaviour</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Default</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdMono}>Tokens__Issuer</td>
            <td class={td}>
              <code class={mono}>netptune.co.uk</code>
            </td>
            <td class={td}>JWT issuer claim.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>Tokens__Audience</td>
            <td class={td}>
              <code class={mono}>netptune.co.uk</code>
            </td>
            <td class={td}>JWT audience claim.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>Tokens__ExpireDays</td>
            <td class={td}>
              <code class={mono}>5</code>
            </td>
            <td class={td}>Number of days before a token expires.</td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>Email</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Required</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdMono}>SEND_GRID_API_KEY</td>
            <td class={td}>Yes</td>
            <td class={td}>SendGrid API key used to send transactional emails.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>Email__DefaultFromAddress</td>
            <td class={td}>Yes</td>
            <td class={td}>
              The <code class={mono}>From</code> address for all outgoing emails. Must be verified
              in SendGrid.
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>Email__DefaultFromDisplayName</td>
            <td class={td}>No</td>
            <td class={td}>Display name shown alongside the from address.</td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>S3 storage</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Required</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_S3_BUCKET_NAME</td>
            <td class={td}>Yes</td>
            <td class={td}>Name of the S3 bucket for file attachments.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_S3_REGION</td>
            <td class={td}>Yes</td>
            <td class={td}>
              AWS region (e.g. <code class={mono}>us-east-1</code>) or custom endpoint region for
              MinIO.
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_S3_ACCESS_KEY_ID</td>
            <td class={td}>Yes</td>
            <td class={td}>AWS access key ID or MinIO access key.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>NETPTUNE_S3_SECRET_ACCESS_KEY</td>
            <td class={td}>Yes</td>
            <td class={td}>AWS secret access key or MinIO secret key.</td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>ASP.NET Core</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Example</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdMono}>ASPNETCORE_URLS</td>
            <td class={td}>
              <code class={mono}>http://0.0.0.0:7400</code>
            </td>
            <td class={td}>Addresses the API listens on.</td>
          </tr>
          <tr class={tr}>
            <td class={tdMono}>ASPNETCORE_FORWARDEDHEADERS_ENABLED</td>
            <td class={td}>
              <code class={mono}>true</code>
            </td>
            <td class={td}>
              Enable processing of <code class={mono}>X-Forwarded-For</code> and{' '}
              <code class={mono}>X-Forwarded-Proto</code> headers. Always set to{' '}
              <code class={mono}>true</code> when behind a reverse proxy or ingress.
            </td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>CORS</h3>
      <p class={p}>
        CORS origins are configured via <code class={mono}>appsettings.json</code>. When
        self-hosting, set <code class={mono}>CorsOrigins</code> to include your public domain:
      </p>
      <CodeBlock language="json">{`{
  "CorsOrigins": [
    "https://your-domain.com"
  ]
}`}</CodeBlock>

      {/* ── Job Server ── */}
      <h2 class={h2}>Job server</h2>
      <p class={p}>
        The Job Server requires the same connection string, authentication, email, and S3 variables
        as the API server. The table below lists each one for reference.
      </p>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <For
            each={[
              ['ConnectionStrings__netptune', 'PostgreSQL connection string (same as API).'],
              ['ConnectionStrings__cache', 'Redis / Valkey connection string (same as API).'],
              ['ConnectionStrings__nats', 'NATS server URL (same as API).'],
              ['NETPTUNE_SIGNING_KEY', 'JWT signing key (same as API).'],
              ['SEND_GRID_API_KEY', 'SendGrid API key.'],
              ['NETPTUNE_S3_BUCKET_NAME', 'S3 bucket name.'],
              ['NETPTUNE_S3_REGION', 'S3 region.'],
              ['NETPTUNE_S3_ACCESS_KEY_ID', 'S3 access key ID.'],
              ['NETPTUNE_S3_SECRET_ACCESS_KEY', 'S3 secret key.'],
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

      {/* ── PostgreSQL ── */}
      <h2 class={h2}>PostgreSQL</h2>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Variable</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <For
            each={[
              ['POSTGRES_USER', 'Database superuser — use postgres.'],
              ['POSTGRES_PASSWORD', 'Superuser password. Use a strong random value.'],
              ['POSTGRES_DB', 'Database name — must be netptune.'],
              ['POSTGRES_HOST_AUTH_METHOD', 'Set to scram-sha-256 for encrypted authentication.'],
              ['POSTGRES_INITDB_ARGS', '--auth-host=scram-sha-256 --auth-local=scram-sha-256'],
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

      {/* ── Redis / Valkey ── */}
      <h2 class={h2}>Redis / Valkey</h2>
      <p class={p}>
        Start the server with a <code class={mono}>--requirepass</code> flag:
      </p>
      <CodeBlock language="bash">{`valkey-server --requirepass <your_password>`}</CodeBlock>
      <p class={p}>
        Include the password in the connection string passed to the API and Job Server:
      </p>
      <CodeBlock>{`<password>@<hostname>:<port>`}</CodeBlock>

      {/* ── NATS ── */}
      <h2 class={h2}>NATS</h2>
      <p class={p}>
        NATS must run with JetStream enabled using the <code class={mono}>-js</code> flag:
      </p>
      <CodeBlock language="bash">{`nats-server -js`}</CodeBlock>
      <Callout type="note">
        No password is required for a basic self-hosted setup. For production environments,
        authentication can be added via NATS configuration files.
      </Callout>

      {/* ── Client ── */}
      <h2 class={h2}>Client (Nginx)</h2>
      <p class={p}>
        The client container is a pre-built Angular application served by Nginx. No additional
        configuration is required. The following behaviours are baked in:
      </p>
      <ul class="mb-4 space-y-1.5 text-sm leading-relaxed text-slate-600 dark:text-white/65">
        <li>
          All requests to <code class={mono}>/api/*</code> are proxied to the API server
        </li>
        <li>All other routes fall back to the Angular SPA's index.html</li>
        <li>Static assets are served with a 1-year Cache-Control header</li>
        <li>
          Security headers: <code class={mono}>X-Frame-Options</code>,{' '}
          <code class={mono}>X-Content-Type-Options</code>,{' '}
          <code class={mono}>Referrer-Policy</code>
        </li>
        <li>Gzip compression enabled</li>
      </ul>

      {/* ── Helm mapping ── */}
      <h2 class={h2}>Helm values mapping</h2>
      <p class={p}>
        When deploying via Helm, environment variables are managed through{' '}
        <code class={mono}>values.yaml</code> and <code class={mono}>values.secret.yaml</code>. The
        table below maps Helm value paths to their corresponding environment variables.
      </p>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Helm path</th>
            <th class={th}>Environment variable</th>
          </tr>
        </thead>
        <tbody>
          <For
            each={[
              ['secrets.api.signing_key', 'NETPTUNE_SIGNING_KEY'],
              ['secrets.api.github_client_id', 'NETPTUNE_GITHUB_CLIENT_ID'],
              ['secrets.api.github_secret', 'NETPTUNE_GITHUB_SECRET'],
              ['secrets.api.sendgrid_api_key', 'SEND_GRID_API_KEY'],
              ['secrets.api.s3_bucket_name', 'NETPTUNE_S3_BUCKET_NAME'],
              ['secrets.api.s3_region', 'NETPTUNE_S3_REGION'],
              ['secrets.api.s3_access_key_id', 'NETPTUNE_S3_ACCESS_KEY_ID'],
              ['secrets.api.s3_secret_access_key', 'NETPTUNE_S3_SECRET_ACCESS_KEY'],
              ['secrets.postgres.postgres_password', 'POSTGRES_PASSWORD'],
              ['secrets.cache.cache_password', 'Redis --requirepass value'],
            ]}
          >
            {([helm, env]) => (
              <tr class={tr}>
                <td class={tdMono}>{helm}</td>
                <td class={tdFirst}>{env}</td>
              </tr>
            )}
          </For>
        </tbody>
      </DocTable>

      <DocPagination
        prev={{ href: '/docs/kubernetes', label: 'Kubernetes / Helm' }}
        next={{ href: '/docs/external-services', label: 'External Services' }}
      />
    </DocLayout>
  );
}
