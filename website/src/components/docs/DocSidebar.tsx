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
            <p class="mb-2 text-xs font-semibold tracking-wider text-slate-400 uppercase dark:text-white/35">
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
                            ? 'bg-violet-50 font-medium text-brand dark:bg-violet-500/10 dark:text-violet-300'
                            : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900 dark:text-white/55 dark:hover:bg-white/5 dark:hover:text-white'
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
