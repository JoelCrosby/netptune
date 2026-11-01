import { For } from 'solid-js';
import { useLocation } from '@solidjs/router';
import { docsNav } from '~/data/docs-nav';

export default function DocSidebar() {
  const location = useLocation();

  return (
    <nav class="flex flex-col gap-7">
      <For each={docsNav}>
        {(section, index) => (
          <div class="flex flex-col gap-1">
            <p class="m-0 flex items-baseline gap-2 px-3 pb-1.5 font-geist-mono text-[11px] tracking-[0.08em] text-[#8b83a0] uppercase">
              <span class="shrink-0 text-violet-400">{String(index() + 1).padStart(2, '0')}</span>
              <span class="whitespace-nowrap">{section.heading}</span>
            </p>
            <For each={section.items}>
              {(item) => {
                const isActive = () => location.pathname === item.href;
                return (
                  <a
                    href={item.href}
                    aria-current={isActive() ? 'page' : undefined}
                    class="block rounded-full px-3 py-2 text-sm transition-colors"
                    classList={{
                      'bg-ink font-semibold text-white shadow-[0_6px_18px_rgb(20_15_34/0.22)]':
                        isActive(),
                      'text-ink-muted hover:bg-[#efebf6] hover:text-ink': !isActive(),
                    }}
                  >
                    {item.label}
                  </a>
                );
              }}
            </For>
          </div>
        )}
      </For>
    </nav>
  );
}
