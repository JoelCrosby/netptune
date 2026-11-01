import { For } from 'solid-js';
import { useLocation } from '@solidjs/router';

const sections = [
  {
    heading: 'Getting Started',
    items: [{ href: '/docs', label: 'Overview' }],
  },
  {
    heading: 'AI Assistant',
    items: [
      { href: '/docs/assistant', label: 'Overview' },
      { href: '/docs/assistant/asking', label: 'Asking questions' },
      { href: '/docs/assistant/proposals', label: 'Proposals and review' },
      { href: '/docs/assistant/writing', label: 'Writing in tasks and pages' },
      { href: '/docs/assistant/task-review', label: 'Task review' },
      { href: '/docs/assistant/access-and-cost', label: 'Access, privacy and cost' },
    ],
  },
  {
    heading: 'Features',
    items: [
      { href: '/docs/pages', label: 'Pages' },
      { href: '/docs/source-control', label: 'Source control' },
      { href: '/docs/queries', label: 'Queries' },
      { href: '/docs/import-export', label: 'Import & export' },
      { href: '/docs/automations', label: 'Automations' },
    ],
  },
  {
    heading: 'Reference',
    items: [{ href: '/docs/api', label: 'API' }],
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
