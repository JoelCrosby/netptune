import { For } from 'solid-js';
import Callout from '~/components/docs/Callout';
import CodeBlock from '~/components/docs/CodeBlock';
import DocLayout from '~/components/docs/DocLayout';
import DocPagination from '~/components/docs/DocPagination';
import DocTable from '~/components/docs/DocTable';

const h2 = 'mt-12 mb-4 text-xl font-semibold text-slate-900 dark:text-white';
const h3 = 'mt-8 mb-3 text-base font-semibold text-slate-900 dark:text-white';
const p = 'mb-4 leading-7 text-slate-600 dark:text-white/65';
const step = 'mb-2 flex items-center gap-3 text-base font-semibold text-slate-900 dark:text-white';
const stepNum =
  'flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-brand text-xs font-bold text-white';
const th =
  'px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-white/40';
const td = 'px-4 py-3 text-slate-600 dark:text-white/65 align-top';
const tdFirst = 'px-4 py-3 font-medium text-slate-800 dark:text-white/85 align-top';
const tr = 'border-b border-slate-100 last:border-0 dark:border-white/5';
const thead = 'border-b border-slate-200 bg-slate-50 dark:border-white/10 dark:bg-white/5';

export default function KubernetesPage() {
  return (
    <DocLayout
      title="Kubernetes / Helm"
      description="Deploy Netptune to a Kubernetes cluster using the official Helm chart."
    >
      <p class={p}>
        The Helm chart is the recommended deployment method for production. It manages all
        Kubernetes resources, handles secret injection, and supports TLS via cert-manager with
        automatic Let's Encrypt certificate renewal.
      </p>

      <h2 class={h2}>Prerequisites</h2>
      <ul class="mb-4 space-y-1.5 text-sm text-slate-600 dark:text-white/65">
        <li>A running Kubernetes cluster (k3s, EKS, GKE, etc.)</li>
        <li>
          <code class="font-mono text-[13px]">kubectl</code> configured with cluster access
        </li>
        <li>
          <code class="font-mono text-[13px]">helm</code> 3.x installed
        </li>
        <li>
          <a
            href="https://kubernetes.github.io/ingress-nginx/"
            target="_blank"
            rel="noopener noreferrer"
            class="text-brand underline underline-offset-2 hover:text-brand-dark"
          >
            NGINX Ingress Controller
          </a>{' '}
          installed in the cluster
        </li>
        <li>
          <a
            href="https://cert-manager.io/"
            target="_blank"
            rel="noopener noreferrer"
            class="text-brand underline underline-offset-2 hover:text-brand-dark"
          >
            cert-manager
          </a>{' '}
          installed for automatic TLS
        </li>
        <li>A domain name pointed at your cluster's external IP or load balancer</li>
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
          Clone the repository
        </p>
        <p class={p}>
          The Helm chart lives in the <code class="font-mono text-[13px]">charts/</code> directory
          of the Netptune repository:
        </p>
        <CodeBlock language="bash">{`git clone https://github.com/JoelCrosby/netptune.git
cd netptune`}</CodeBlock>
      </div>

      {/* Step 2 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>2</span>
          Review default values
        </p>
        <p class={p}>
          Inspect <code class="font-mono text-[13px]">charts/netptune/values.yaml</code> and note
          the key sections you will need to override:
        </p>
        <CodeBlock language="yaml">{`ingress:
  enabled: true
  host: "netptune.co.uk"       # replace with your domain
  tls:
    enabled: true
    email: "your@email.com"    # used for Let's Encrypt registration

persistence:
  postgres:
    size: "40Gi"               # adjust to your storage needs

parameters:
  api:
    api_image: "ghcr.io/joelcrosby/netptune:latest"
    port_http: 7400
  jobs:
    jobs_image: "ghcr.io/joelcrosby/netptune-jobs:latest"
    port_http: 8080
  client:
    client_image: "ghcr.io/joelcrosby/netptune-client:latest"
    api_url: "http://api-service:7400"`}</CodeBlock>
      </div>

      {/* Step 3 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>3</span>
          Create a secrets file
        </p>
        <p class={p}>Copy the example secrets file and fill in your credentials:</p>
        <CodeBlock language="bash">{`cp charts/netptune/values.secret.yaml.example charts/netptune/values.secret.yaml`}</CodeBlock>
        <p class={p}>
          Edit <code class="font-mono text-[13px]">values.secret.yaml</code>:
        </p>
        <CodeBlock language="yaml">{`secrets:
  postgres:
    postgres_password: "change_me_strong_password"

  cache:
    cache_password: "change_me_redis_password"

  api:
    # Generate with: openssl rand -base64 64
    signing_key: "change_me_signing_key"

    # GitHub OAuth (leave blank to disable)
    github_client_id: ""
    github_secret: ""

    # SendGrid
    sendgrid_api_key: "SG.xxxxxx"

    # S3-compatible storage
    s3_bucket_name: "netptune"
    s3_region: "us-east-1"
    s3_access_key_id: ""
    s3_secret_access_key: ""`}</CodeBlock>
        <Callout type="warning">
          Never commit <code class="font-mono text-[13px]">values.secret.yaml</code> to source
          control. Add it to your <code class="font-mono text-[13px]">.gitignore</code>.
        </Callout>
      </div>

      {/* Step 4 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>4</span>
          Install the chart
        </p>
        <p class={p}>
          Run <code class="font-mono text-[13px]">helm upgrade --install</code> to deploy or upgrade
          the release:
        </p>
        <CodeBlock language="bash">{`helm upgrade --install netptune-app charts/netptune/ \\
  --namespace netptune \\
  --create-namespace \\
  --values charts/netptune/values.yaml \\
  --values charts/netptune/values.secret.yaml \\
  --set ingress.host=your-domain.com \\
  --set ingress.tls.email=your@email.com`}</CodeBlock>
        <p class={p}>
          Alternatively, pass secrets directly on the command line (useful for CI/CD pipelines):
        </p>
        <CodeBlock language="bash">{`helm upgrade --install netptune-app charts/netptune/ \\
  --namespace netptune \\
  --create-namespace \\
  --set ingress.host=your-domain.com \\
  --set ingress.tls.email=your@email.com \\
  --set secrets.postgres.postgres_password="<password>" \\
  --set secrets.cache.cache_password="<password>" \\
  --set secrets.api.signing_key="<signing-key>" \\
  --set secrets.api.sendgrid_api_key="<sendgrid-key>" \\
  --set secrets.api.s3_bucket_name="<bucket>" \\
  --set secrets.api.s3_region="<region>" \\
  --set secrets.api.s3_access_key_id="<key-id>" \\
  --set secrets.api.s3_secret_access_key="<secret>"`}</CodeBlock>
      </div>

      {/* Step 5 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>5</span>
          Verify the deployment
        </p>
        <p class={p}>Check that all pods are running:</p>
        <CodeBlock language="bash">{`kubectl get pods -n netptune`}</CodeBlock>
        <p class={p}>
          Expected output — all pods should show <code class="font-mono text-[13px]">Running</code>:
        </p>
        <CodeBlock>{`NAME                       READY   STATUS    RESTARTS   AGE
api-xxxxxxxxx-xxxxx        1/1     Running   0          2m
jobs-xxxxxxxxx-xxxxx       1/1     Running   0          2m
client-xxxxxxxxx-xxxxx     1/1     Running   0          2m
postgres-0                 1/1     Running   0          2m
cache-0                    1/1     Running   0          2m
nats-xxxxxxxxx-xxxxx       1/1     Running   0          2m`}</CodeBlock>
        <p class={p}>Check the ingress for the assigned address:</p>
        <CodeBlock language="bash">{`kubectl get ingress -n netptune`}</CodeBlock>
        <p class={p}>
          Once the <code class="font-mono text-[13px]">ADDRESS</code> field is populated and DNS has
          propagated, Netptune will be accessible at{' '}
          <code class="font-mono text-[13px]">https://your-domain.com</code>.
        </p>
      </div>

      {/* Step 6 */}
      <div class="mt-8">
        <p class={step}>
          <span class={stepNum}>6</span>
          Verify TLS
        </p>
        <p class={p}>
          cert-manager will automatically request a Let's Encrypt certificate. Check the certificate
          status:
        </p>
        <CodeBlock language="bash">{`kubectl get certificate -n netptune`}</CodeBlock>
        <p class={p}>
          The <code class="font-mono text-[13px]">READY</code> column should show{' '}
          <code class="font-mono text-[13px]">True</code> within a few minutes.
        </p>
      </div>

      <h2 class={h2}>Kubernetes resources</h2>
      <p class={p}>The Helm chart deploys the following resources:</p>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Resource</th>
            <th class={th}>Kind</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <For
            each={[
              ['api', 'Deployment', 'ASP.NET Core API server'],
              ['jobs', 'Deployment', 'Background job server'],
              ['client', 'Deployment', 'Angular frontend + Nginx'],
              ['nats', 'Deployment', 'NATS event messaging (JetStream enabled)'],
              ['postgres', 'StatefulSet', 'PostgreSQL with persistent volume'],
              ['cache', 'StatefulSet', 'Valkey / Redis with persistent volume'],
              ['*-service', 'Service', 'ClusterIP services for internal communication'],
              ['netptune-ingress', 'Ingress', 'NGINX Ingress with TLS termination'],
              ['letsencrypt', 'ClusterIssuer', "cert-manager issuer for Let's Encrypt"],
              ['*-secret', 'Secret', 'Passwords and API keys'],
              ['*-config', 'ConfigMap', 'Non-sensitive configuration'],
            ]}
          >
            {([name, kind, desc]) => (
              <tr class={tr}>
                <td class={tdFirst}>
                  <code class="font-mono text-[13px]">{name}</code>
                </td>
                <td class={td}>{kind}</td>
                <td class={td}>{desc}</td>
              </tr>
            )}
          </For>
        </tbody>
      </DocTable>

      <h2 class={h2}>Optional monitoring stack</h2>
      <p class={p}>
        The chart includes optional monitoring components. Enable them in{' '}
        <code class="font-mono text-[13px]">values.yaml</code>:
      </p>
      <CodeBlock language="yaml">{`monitoring:
  prometheus:
    enabled: false    # metrics collection
  grafana:
    enabled: false    # dashboards
  loki:
    enabled: false    # log aggregation
  otelCollector:
    enabled: false    # distributed tracing`}</CodeBlock>
      <p class={p}>A PgAdmin instance for database inspection can also be enabled:</p>
      <CodeBlock language="yaml">{`pgadmin:
  enabled: false
  email: "admin@your-domain.com"
  password: "change_me"`}</CodeBlock>

      <h2 class={h2}>Updating</h2>
      <p class={p}>
        Re-run the <code class="font-mono text-[13px]">helm upgrade</code> command with your values
        files. Database migrations run automatically when the API pod starts.
      </p>
      <CodeBlock language="bash">{`helm upgrade netptune-app charts/netptune/ \\
  --namespace netptune \\
  --values charts/netptune/values.yaml \\
  --values charts/netptune/values.secret.yaml`}</CodeBlock>

      <h2 class={h2}>Uninstalling</h2>
      <CodeBlock language="bash">{`helm uninstall netptune-app -n netptune`}</CodeBlock>
      <p class={p}>
        This removes all Kubernetes resources but preserves persistent volumes. To delete volumes as
        well:
      </p>
      <CodeBlock language="bash">{`kubectl delete pvc --all -n netptune`}</CodeBlock>

      <h2 class={h2}>Troubleshooting</h2>

      <h3 class={h3}>Pods stuck in Pending</h3>
      <p class={p}>Check events for the pod:</p>
      <CodeBlock language="bash">{`kubectl describe pod <pod-name> -n netptune`}</CodeBlock>
      <p class={p}>
        Common causes: insufficient cluster resources, or a PersistentVolumeClaim that cannot be
        satisfied because no StorageClass is available.
      </p>

      <h3 class={h3}>API pod crashes on startup</h3>
      <CodeBlock language="bash">{`kubectl logs deployment/api -n netptune`}</CodeBlock>
      <p class={p}>
        Verify all required secrets are present and the PostgreSQL StatefulSet is healthy before the
        API deployment starts.
      </p>

      <h3 class={h3}>Certificate not issued</h3>
      <CodeBlock language="bash">{`kubectl describe certificate -n netptune
kubectl describe clusterissuer letsencrypt -n netptune`}</CodeBlock>
      <p class={p}>
        Ensure the ingress host DNS record resolves to your cluster's external IP before
        cert-manager can complete the ACME challenge.
      </p>

      <h3 class={h3}>Ingress has no address</h3>
      <p class={p}>Confirm the NGINX Ingress Controller is installed and running:</p>
      <CodeBlock language="bash">{`kubectl get pods -n ingress-nginx`}</CodeBlock>

      <DocPagination
        prev={{ href: '/docs/docker-compose', label: 'Docker Compose' }}
        next={{ href: '/docs/configuration', label: 'Configuration' }}
      />
    </DocLayout>
  );
}
