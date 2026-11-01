import { For, Show, createSignal } from 'solid-js';
import { Menu, X } from 'lucide-solid';
import Button from './Button';
import ThemeToggle from './ThemeToggle';

const navLinks = [
  { href: '/#features', label: 'Features' },
  { href: '/assistant', label: 'Assistant' },
  { href: '/docs', label: 'Docs' },
];

export default function Nav() {
  const [menuOpen, setMenuOpen] = createSignal(false);

  return (
    <header class="sticky top-0 z-50 border-b border-slate-100 bg-white/90 backdrop-blur-sm dark:border-white/10 dark:bg-black/90">
      <div class="mx-auto flex h-16 max-w-6xl items-center justify-between gap-8 px-6">
        <a href="/" class="flex shrink-0 items-center gap-2">
          <img class="h-10 w-10 p-1 bg-linear-to-br from-brand/40 via-fuchsia-300/30 to-sky-300/30 rounded-lg" src="/favicon.svg" alt="Netptune logo" />

          <span class="text-xl tracking-tight text-slate-900 dark:text-white">
            Netptune
          </span>
        </a>

        <nav class="hidden items-center gap-7 md:flex">
          <For each={navLinks}>
            {(link) => (
              <a
                href={link.href}
                class="text-sm text-slate-500 transition-colors hover:text-slate-900 dark:text-white/55 dark:hover:text-white"
              >
                {link.label}
              </a>
            )}
          </For>
        </nav>

        <div class="flex shrink-0 items-center gap-2">
          <ThemeToggle />
          <div class="hidden sm:block">
            <Button variant="primary" size="sm" href="https://app.netptune.co.uk/auth/login">
              Open Netptune
            </Button>
          </div>
          <button
            type="button"
            class="flex h-9 w-9 items-center justify-center rounded-full text-slate-600 transition-colors hover:bg-violet-50 hover:text-brand md:hidden dark:text-white/70 dark:hover:bg-violet-500/10"
            aria-label={menuOpen() ? 'Close menu' : 'Open menu'}
            aria-expanded={menuOpen()}
            aria-controls="mobile-nav"
            onClick={() => setMenuOpen(!menuOpen())}
          >
            <Show when={menuOpen()} fallback={<Menu size={20} />}>
              <X size={20} />
            </Show>
          </button>
        </div>
      </div>

      <Show when={menuOpen()}>
        <nav
          id="mobile-nav"
          class="border-t border-slate-100 px-6 py-3 md:hidden dark:border-white/10"
        >
          <For each={navLinks}>
            {(link) => (
              <a
                href={link.href}
                class="block rounded-lg px-3 py-2.5 text-base text-slate-700 transition-colors hover:bg-violet-50 hover:text-brand dark:text-white/75 dark:hover:bg-violet-500/10 dark:hover:text-white"
                onClick={() => setMenuOpen(false)}
              >
                {link.label}
              </a>
            )}
          </For>
          <div class="mt-2 px-3 pb-2 sm:hidden">
            <Button
              variant="primary"
              size="md"
              href="https://app.netptune.co.uk/auth/login"
              class="w-full"
            >
              Open Netptune
            </Button>
          </div>
        </nav>
      </Show>
    </header>
  );
}
