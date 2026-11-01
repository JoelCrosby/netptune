import { For, Show, createSignal } from 'solid-js';
import { useLocation } from '@solidjs/router';
import { Menu, X } from 'lucide-solid';

const navLinks = [
  { href: '/#features', label: 'Features' },
  { href: '/assistant', label: 'Assistant' },
  { href: '/#code', label: 'GitHub' },
  { href: '/docs', label: 'Docs' },
];

export default function Nav() {
  const [menuOpen, setMenuOpen] = createSignal(false);
  const location = useLocation();

  function isCurrentPage(href: string): boolean {
    if (href.includes('#')) return false;

    return location.pathname === href || location.pathname.startsWith(`${href}/`);
  }

  return (
    <header class="sticky top-0 z-50 border-b border-purple-200/8 bg-[rgb(8_5_16/0.78)] font-geist backdrop-blur-[14px]">
      <div class="mx-auto flex h-16 max-w-310 items-center justify-between gap-6 px-6">
        <a href="/" class="flex shrink-0 items-center gap-2.5 text-white">
          <img src="/favicon.svg" alt="Netptune logo" class="size-7.5 rounded-lg" />
          <span class="text-lg font-semibold tracking-[-0.02em]">Netptune</span>
        </a>

        <nav class="hidden items-center gap-7 text-sm md:flex">
          <For each={navLinks}>
            {(link) => (
              <a
                href={link.href}
                aria-current={isCurrentPage(link.href) ? 'page' : undefined}
                class="text-white/62 transition-colors hover:text-white aria-[current=page]:text-white"
              >
                {link.label}
              </a>
            )}
          </For>
        </nav>

        <div class="flex shrink-0 items-center gap-2">
          <a
            href="https://app.netptune.co.uk/auth/login"
            class="hidden px-3.5 py-2 text-sm text-white/75 transition-colors hover:text-white sm:block"
          >
            Sign in
          </a>
          <a
            href="https://app.netptune.co.uk/auth/register"
            class="rounded-full bg-brand px-4 py-2.25 text-sm font-medium text-white shadow-[inset_0_0_0_1px_rgb(233_213_255/0.25)] transition-colors hover:bg-[#7c4dd0]"
          >
            Start free
          </a>
          <button
            type="button"
            class="flex size-9 items-center justify-center rounded-full text-white/70 transition-colors hover:bg-white/10 hover:text-white md:hidden"
            aria-label={menuOpen() ? 'Close menu' : 'Open menu'}
            aria-expanded={menuOpen()}
            aria-controls="site-mobile-nav"
            onClick={() => setMenuOpen(!menuOpen())}
          >
            <Show when={menuOpen()} fallback={<Menu size={20} />}>
              <X size={20} />
            </Show>
          </button>
        </div>
      </div>

      <Show when={menuOpen()}>
        <nav id="site-mobile-nav" class="border-t border-purple-200/8 px-6 py-3 md:hidden">
          <For each={navLinks}>
            {(link) => (
              <a
                href={link.href}
                aria-current={isCurrentPage(link.href) ? 'page' : undefined}
                class="block rounded-lg px-3 py-2.5 text-base text-white/75 transition-colors hover:bg-white/8 hover:text-white aria-[current=page]:text-white"
                onClick={() => setMenuOpen(false)}
              >
                {link.label}
              </a>
            )}
          </For>
          <a
            href="https://app.netptune.co.uk/auth/login"
            class="block rounded-lg px-3 py-2.5 text-base text-white/75 transition-colors hover:bg-white/8 hover:text-white sm:hidden"
          >
            Sign in
          </a>
        </nav>
      </Show>
    </header>
  );
}
