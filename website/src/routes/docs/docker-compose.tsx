import Callout from '~/components/docs/Callout';
import CodeBlock from '~/components/docs/CodeBlock';
import DocLayout from '~/components/docs/DocLayout';
import DocPagination from '~/components/docs/DocPagination';

const h2 = 'mt-12 mb-4 text-xl font-semibold text-slate-900 dark:text-white';
const h3 = 'mt-8 mb-3 text-base font-semibold text-slate-900 dark:text-white';
const p = 'mb-4 leading-7 text-slate-600 dark:text-white/65';
const step = 'mb-2 flex items-center gap-3 text-base font-semibold text-slate-900 dark:text-white';
const stepNum =
  'flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-brand text-xs font-bold text-white';

export default function DockerComposePage() {
  return (
    <DocLayout
      title="Docker Compose"
      description="Deploy Netptune on a single machine using Docker Compose."
    >
      <p class={p}>
        Docker Compose is the simplest way to get a self-hosted Netptune instance running. All six
        services are defined in a single file and start with one command.
      </p>

      <h2 class={h2}>Prerequisites</h2>
      <ul class="mb-4 space-y-1.5 text-sm text-slate-600 dark:text-white/65">
        <li>Docker Engine 24+ and Docker Compose v2</li>
        <li>A domain name or static IP address</li>
        <li>
          Credentials for all{' '}
          <a
            href="/docs/external-services"
            class="text-brand underline underline-offset-2 hover:text-brand-dark"
          >
            external services
          </a>
        </li>
      </ul>

      <h2 class={h2}>Installation</h2>

      {/* Step 1 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>1</span>
          Create a working directory
        </p>
        <p class={p}>Create a directory to hold your Netptune deployment files.</p>
        <CodeBlock language="bash">{`mkdir netptune && cd netptune`}</CodeBlock>
        <p class={p}>You will create the following two files inside this directory:</p>
        <CodeBlock>{`netptune/
├── docker-compose.yml
└── .env`}</CodeBlock>
      </div>

      {/* Step 2 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>2</span>
          Create the Compose file
        </p>
        <p class={p}>
          Create <code class="font-mono text-[13px]">docker-compose.yml</code> with the following
          content:
        </p>
        <CodeBlock language="yaml">{`services:

  client:
    image: ghcr.io/joelcrosby/netptune-client:latest
    restart: unless-stopped
    ports:
      - "80:80"
    depends_on:
      - api

  api:
    image: ghcr.io/joelcrosby/netptune:latest
    restart: unless-stopped
    environment:
      ASPNETCORE_URLS: http://0.0.0.0:7400
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
      ConnectionStrings__netptune: "Host=postgres;Port=5432;Username=postgres;Password=\${POSTGRES_PASSWORD};Database=netptune"
      ConnectionStrings__cache: "\${REDIS_PASSWORD}@cache:6379"
      ConnectionStrings__nats: "nats://nats:4222"
      NETPTUNE_SIGNING_KEY: "\${SIGNING_KEY}"
      NETPTUNE_GITHUB_CLIENT_ID: "\${GITHUB_CLIENT_ID}"
      NETPTUNE_GITHUB_SECRET: "\${GITHUB_SECRET}"
      SEND_GRID_API_KEY: "\${SENDGRID_API_KEY}"
      NETPTUNE_S3_BUCKET_NAME: "\${S3_BUCKET_NAME}"
      NETPTUNE_S3_REGION: "\${S3_REGION}"
      NETPTUNE_S3_ACCESS_KEY_ID: "\${S3_ACCESS_KEY_ID}"
      NETPTUNE_S3_SECRET_ACCESS_KEY: "\${S3_SECRET_ACCESS_KEY}"
    depends_on:
      - postgres
      - cache
      - nats

  jobs:
    image: ghcr.io/joelcrosby/netptune-jobs:latest
    restart: unless-stopped
    environment:
      ConnectionStrings__netptune: "Host=postgres;Port=5432;Username=postgres;Password=\${POSTGRES_PASSWORD};Database=netptune"
      ConnectionStrings__cache: "\${REDIS_PASSWORD}@cache:6379"
      ConnectionStrings__nats: "nats://nats:4222"
      NETPTUNE_SIGNING_KEY: "\${SIGNING_KEY}"
      SEND_GRID_API_KEY: "\${SENDGRID_API_KEY}"
      NETPTUNE_S3_BUCKET_NAME: "\${S3_BUCKET_NAME}"
      NETPTUNE_S3_REGION: "\${S3_REGION}"
      NETPTUNE_S3_ACCESS_KEY_ID: "\${S3_ACCESS_KEY_ID}"
      NETPTUNE_S3_SECRET_ACCESS_KEY: "\${S3_SECRET_ACCESS_KEY}"
    depends_on:
      - postgres
      - cache
      - nats

  postgres:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: "\${POSTGRES_PASSWORD}"
      POSTGRES_DB: netptune
      POSTGRES_HOST_AUTH_METHOD: scram-sha-256
      POSTGRES_INITDB_ARGS: "--auth-host=scram-sha-256 --auth-local=scram-sha-256"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  cache:
    image: valkey/valkey:9-alpine
    restart: unless-stopped
    command: ["valkey-server", "--requirepass", "\${REDIS_PASSWORD}"]
    volumes:
      - cache_data:/data

  nats:
    image: nats:alpine
    restart: unless-stopped
    command: ["-js"]

volumes:
  postgres_data:
  cache_data:`}</CodeBlock>
      </div>

      {/* Step 3 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>3</span>
          Create the environment file
        </p>
        <p class={p}>
          Create a <code class="font-mono text-[13px]">.env</code> file in the same directory. Never
          commit this file to source control.
        </p>
        <CodeBlock language="env">{`# Database
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
S3_SECRET_ACCESS_KEY=`}</CodeBlock>
        <p class={p}>Generate a secure signing key with:</p>
        <CodeBlock language="bash">{`openssl rand -base64 64`}</CodeBlock>
      </div>

      {/* Step 4 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>4</span>
          Start the stack
        </p>
        <p class={p}>Pull all images and start the services in the background:</p>
        <CodeBlock language="bash">{`docker compose up -d`}</CodeBlock>
        <p class={p}>Check that all six containers are running:</p>
        <CodeBlock language="bash">{`docker compose ps`}</CodeBlock>
        <p class={p}>
          You should see <code class="font-mono text-[13px]">client</code>,{' '}
          <code class="font-mono text-[13px]">api</code>,{' '}
          <code class="font-mono text-[13px]">jobs</code>,{' '}
          <code class="font-mono text-[13px]">postgres</code>,{' '}
          <code class="font-mono text-[13px]">cache</code>, and{' '}
          <code class="font-mono text-[13px]">nats</code> all in the{' '}
          <code class="font-mono text-[13px]">running</code> state.
        </p>
      </div>

      {/* Step 5 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>5</span>
          Verify the deployment
        </p>
        <p class={p}>Tail the API logs to confirm it started and connected to the database:</p>
        <CodeBlock language="bash">{`docker compose logs -f api`}</CodeBlock>
        <p class={p}>Look for output similar to:</p>
        <CodeBlock>{`info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:7400
info: Microsoft.Hosting.Lifetime[0]
      Application started.`}</CodeBlock>
        <p class={p}>
          Open your browser and navigate to{' '}
          <code class="font-mono text-[13px]">http://&lt;your-server-ip&gt;</code> to access the
          Netptune UI.
        </p>
      </div>

      {/* Step 6 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>6</span>
          Configure TLS (recommended)
        </p>
        <p class={p}>
          For production use, place a reverse proxy in front of the client container to terminate
          TLS. A minimal Caddy setup will automatically obtain and renew a Let's Encrypt
          certificate:
        </p>
        <CodeBlock language="Caddyfile">{`your-domain.com {
    reverse_proxy localhost:80
}`}</CodeBlock>
        <Callout type="note">
          Run Caddy alongside your stack and point your domain's DNS A record at the server. Caddy
          handles ACME challenges and certificate renewal automatically.
        </Callout>
      </div>

      <h2 class={h2}>Updating</h2>
      <p class={p}>
        To update to the latest images, pull and restart. Database migrations run automatically on
        API startup — no manual steps are required.
      </p>
      <CodeBlock language="bash">{`docker compose pull
docker compose up -d`}</CodeBlock>

      <h2 class={h2}>Stopping and removing</h2>
      <p class={p}>Stop without removing data:</p>
      <CodeBlock language="bash">{`docker compose down`}</CodeBlock>
      <p class={p}>Stop and remove all data volumes (destructive):</p>
      <CodeBlock language="bash">{`docker compose down -v`}</CodeBlock>

      <h2 class={h2}>Troubleshooting</h2>

      <h3 class={h3}>API fails to start</h3>
      <p class={p}>
        Check the connection strings. The most common cause is an incorrect password or hostname in{' '}
        <code class="font-mono text-[13px]">ConnectionStrings__netptune</code>.
      </p>
      <CodeBlock language="bash">{`docker compose logs api`}</CodeBlock>

      <h3 class={h3}>Cannot connect to the database</h3>
      <p class={p}>
        Ensure PostgreSQL is healthy before the API starts. Add a health check to the{' '}
        <code class="font-mono text-[13px]">postgres</code> service:
      </p>
      <CodeBlock language="yaml">{`postgres:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres"]
    interval: 5s
    timeout: 5s
    retries: 5`}</CodeBlock>
      <p class={p}>
        Then update the <code class="font-mono text-[13px]">api</code> and{' '}
        <code class="font-mono text-[13px]">jobs</code>{' '}
        <code class="font-mono text-[13px]">depends_on</code> blocks to use{' '}
        <code class="font-mono text-[13px]">condition: service_healthy</code>.
      </p>

      <h3 class={h3}>File uploads fail</h3>
      <p class={p}>
        Verify your S3 credentials and confirm the bucket exists with the correct region set.
      </p>

      <h3 class={h3}>Emails are not sent</h3>
      <p class={p}>
        Check that your SendGrid API key is valid and your sender address is verified in the
        SendGrid dashboard. See the{' '}
        <a
          href="/docs/external-services"
          class="text-brand underline underline-offset-2 hover:text-brand-dark"
        >
          External Services
        </a>{' '}
        guide.
      </p>

      <DocPagination
        prev={{ href: '/docs', label: 'Overview' }}
        next={{ href: '/docs/kubernetes', label: 'Kubernetes / Helm' }}
      />
    </DocLayout>
  );
}
