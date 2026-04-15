import { For } from 'solid-js';
import { ChevronRight, Container, Server } from 'lucide-solid';
import Callout from '~/components/docs/Callout';
import DocLayout from '~/components/docs/DocLayout';
import DocPagination from '~/components/docs/DocPagination';
import DocTable from '~/components/docs/DocTable';

const th =
  'px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-white/40';
const td = 'px-4 py-3 text-slate-600 dark:text-white/65 align-top';
const tdFirst = 'px-4 py-3 font-medium text-slate-800 dark:text-white/85 align-top';
const tr = 'border-b border-slate-100 last:border-0 dark:border-white/5';
const thead = 'border-b border-slate-200 bg-slate-50 dark:border-white/10 dark:bg-white/5';
const h2 = 'mt-12 mb-4 text-xl font-semibold text-slate-900 dark:text-white';
const h3 = 'mt-8 mb-3 text-base font-semibold text-slate-900 dark:text-white';
const p = 'mb-4 leading-7 text-slate-600 dark:text-white/65';

export default function DocsOverview() {
  return (
    <DocLayout
      title="Self-Hosting Guide"
      description="Everything you need to run Netptune on your own infrastructure."
    >
      <p class={p}>
        Netptune is fully open source and designed to be self-hosted. You own your data, your
        instance, and your deployment. This guide covers the complete process from choosing a
        deployment method through to a running production instance.
      </p>

      <Callout type="tip">
        If you just want to get up and running quickly on a single machine, head straight to the{' '}
        <a
          href="/docs/docker-compose"
          class="font-medium text-brand underline underline-offset-2 hover:text-brand-dark"
        >
          Docker Compose
        </a>{' '}
        guide.
      </Callout>

      <h2 class={h2}>Architecture</h2>
      <p class={p}>
        Netptune is composed of three application services backed by three infrastructure
        dependencies.
      </p>

      <h3 class={h3}>Application services</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Service</th>
            <th class={th}>Image</th>
            <th class={th}>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdFirst}>client</td>
            <td class={td}>
              <code class="font-mono text-[13px]">ghcr.io/joelcrosby/netptune-client</code>
            </td>
            <td class={td}>
              Angular frontend served by Nginx. Proxies{' '}
              <code class="font-mono text-[13px]">/api/</code> requests to the API server.
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>api</td>
            <td class={td}>
              <code class="font-mono text-[13px]">ghcr.io/joelcrosby/netptune</code>
            </td>
            <td class={td}>
              ASP.NET Core REST API. Handles authentication, business logic, and real-time events.
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>jobs</td>
            <td class={td}>
              <code class="font-mono text-[13px]">ghcr.io/joelcrosby/netptune-jobs</code>
            </td>
            <td class={td}>
              Background job processor for async work such as email dispatch and event processing.
            </td>
          </tr>
        </tbody>
      </DocTable>

      <h3 class={h3}>Infrastructure dependencies</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Dependency</th>
            <th class={th}>Purpose</th>
            <th class={th}>Required</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdFirst}>PostgreSQL 17</td>
            <td class={td}>Primary database</td>
            <td class={td}>Yes</td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>Redis / Valkey 9</td>
            <td class={td}>Caching and session storage</td>
            <td class={td}>Yes</td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>NATS</td>
            <td class={td}>
              Internal event messaging between services. Must have JetStream enabled.
            </td>
            <td class={td}>Yes</td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>S3-compatible storage</td>
            <td class={td}>File attachments (AWS S3, MinIO, etc.)</td>
            <td class={td}>Yes</td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>SendGrid</td>
            <td class={td}>Transactional email for invites and notifications</td>
            <td class={td}>Yes</td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>GitHub OAuth App</td>
            <td class={td}>Social login via GitHub</td>
            <td class={td}>Optional</td>
          </tr>
        </tbody>
      </DocTable>

      <h2 class={h2}>Prerequisites</h2>

      <h3 class={h3}>Accounts and credentials</h3>
      <p class={p}>
        Before deploying, ensure you have the following external accounts and credentials ready:
      </p>
      <ul class="mb-6 space-y-2.5">
        <For
          each={[
            'SendGrid account with an API key and a verified sender email address',
            'S3-compatible storage — an AWS S3 bucket or a self-hosted MinIO instance',
            'GitHub OAuth App credentials (optional, for GitHub login)',
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

      <h3 class={h3}>Infrastructure</h3>
      <ul class="mb-6 space-y-2.5">
        <For
          each={[
            'A Linux server or Kubernetes cluster with at least 2 vCPUs and 2 GB RAM',
            'A domain name pointed at your server or load balancer',
            "A TLS certificate — Let's Encrypt is supported automatically in the Kubernetes deployment",
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

      <h3 class={h3}>Tools</h3>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Deployment method</th>
            <th class={th}>Required tools</th>
          </tr>
        </thead>
        <tbody>
          <tr class={tr}>
            <td class={tdFirst}>Docker Compose</td>
            <td class={td}>
              <code class="font-mono text-[13px]">docker</code>,{' '}
              <code class="font-mono text-[13px]">docker compose</code>
            </td>
          </tr>
          <tr class={tr}>
            <td class={tdFirst}>Kubernetes</td>
            <td class={td}>
              <code class="font-mono text-[13px]">kubectl</code>,{' '}
              <code class="font-mono text-[13px]">helm</code> 3.x
            </td>
          </tr>
        </tbody>
      </DocTable>

      <h2 class={h2}>Container images</h2>
      <p class={p}>
        All images are published to the GitHub Container Registry and are publicly available.
        Pinning to a specific git SHA tag is recommended for production deployments.
      </p>
      <DocTable>
        <thead class={thead}>
          <tr>
            <th class={th}>Image</th>
            <th class={th}>Tag</th>
          </tr>
        </thead>
        <tbody>
          <For
            each={[
              'ghcr.io/joelcrosby/netptune-client',
              'ghcr.io/joelcrosby/netptune',
              'ghcr.io/joelcrosby/netptune-jobs',
            ]}
          >
            {(image) => (
              <tr class={tr}>
                <td class={td}>
                  <code class="font-mono text-[13px]">{image}</code>
                </td>
                <td class={td}>
                  <code class="font-mono text-[13px]">latest</code>
                </td>
              </tr>
            )}
          </For>
        </tbody>
      </DocTable>

      <h2 class={h2}>Choose a deployment method</h2>
      <div class="mt-6 grid gap-4 sm:grid-cols-2">
        <a
          href="/docs/docker-compose"
          class="group rounded-xl border border-slate-200 p-6 transition-all hover:border-brand hover:shadow-sm dark:border-white/10 dark:hover:border-brand"
        >
          <div class="mb-3 flex items-center gap-3">
            <Container size={20} class="text-brand" />
            <h3 class="font-semibold text-slate-900 dark:text-white">Docker Compose</h3>
          </div>
          <p class="text-sm leading-relaxed text-slate-500 dark:text-white/50">
            Simple single-machine deployment. Best for homelab and small teams.
          </p>
          <div class="mt-4 flex items-center gap-1 text-sm font-medium text-brand">
            Get started <ChevronRight size={14} />
          </div>
        </a>
        <a
          href="/docs/kubernetes"
          class="group rounded-xl border border-slate-200 p-6 transition-all hover:border-brand hover:shadow-sm dark:border-white/10 dark:hover:border-brand"
        >
          <div class="mb-3 flex items-center gap-3">
            <Server size={20} class="text-brand" />
            <h3 class="font-semibold text-slate-900 dark:text-white">Kubernetes / Helm</h3>
          </div>
          <p class="text-sm leading-relaxed text-slate-500 dark:text-white/50">
            Production-grade deployment with rolling updates and persistent storage management.
          </p>
          <div class="mt-4 flex items-center gap-1 text-sm font-medium text-brand">
            Get started <ChevronRight size={14} />
          </div>
        </a>
      </div>

      <DocPagination next={{ href: '/docs/docker-compose', label: 'Docker Compose' }} />
    </DocLayout>
  );
}
