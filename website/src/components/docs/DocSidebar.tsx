import { For } from 'solid-js';
import { useLocation } from '@solidjs/router';

const sections = [
  {
    heading: 'Getting Started',
    items: [{ href: '/docs', label: 'Overview' }],
  },
  {
    heading: 'Deployment',
    items: [
      { href: '/docs/docker-compose', label: 'Docker Compose' },
      { href: '/docs/kubernetes', label: 'Kubernetes / Helm' },
    ],
  },
  {
    heading: 'Reference',
    items: [
      { href: '/docs/configuration', label: 'Configuration' },
      { href: '/docs/external-services', label: 'External Services' },
    ],
  },
];

export default function DocSidebar() {
  const location = useLocation();

  return (
    <nav>
      <For each={sections}>
        {(section) => (
          <div class="mb-6">
            <p class="mb-2 font-mono text-xs font-semibold tracking-wider text-slate-400 uppercase dark:text-white/35">
              {section.heading}
            </p>
            <ul class="space-y-0.5">
              <For each={section.items}>
                {(item) => {
                  const isActive = () => location.pathname === item.href;
                  return (
                    <li>
                      <a
                        href={item.href}
                        class={`block rounded-md px-3 py-1.5 text-sm transition-colors ${
                          isActive()
                            ? 'bg-gradient-to-r from-violet-100 to-fuchsia-50 font-medium text-brand ring-1 ring-brand/10 dark:from-violet-500/20 dark:to-fuchsia-500/10 dark:text-violet-300 dark:ring-white/10'
                            : 'text-slate-600 hover:bg-violet-50/70 hover:text-brand dark:text-white/55 dark:hover:bg-violet-500/10 dark:hover:text-violet-200'
                        }`}
                      >
                        {item.label}
                      </a>
                    </li>
                  );
                }}
              </For>
            </ul>
          </div>
        )}
      </For>
    </nav>
  );
}
