import { Star } from 'lucide-solid';
import { For } from 'solid-js';
import Button from './Button';
import ThemeToggle from './ThemeToggle';

const navLinks = [
  { href: '/#features', label: 'Features' },
  { href: '/#self-host', label: 'Self-host' },
  { href: '/docs', label: 'Docs' },
  {
    href: 'https://github.com/JoelCrosby/netptune',
    label: 'GitHub',
    target: '_blank',
    rel: 'noopener noreferrer',
  },
];

export default function Nav() {
  return (
    <header class="sticky top-0 z-50 border-b border-slate-100 bg-white/90 backdrop-blur-sm dark:border-white/10 dark:bg-black/90">
      <div class="mx-auto flex h-16 max-w-6xl items-center justify-between gap-8 px-6">
        <a href="/" class="flex shrink-0 items-center gap-2">
          <img
            class="flex h-7 w-7 items-center justify-center rounded-md bg-brand text-white"
            src="/netptune-logo.webp"
            alt="Netptune Icon"
          />

          <span class="text-[15px] font-semibold tracking-tight text-slate-900 dark:text-white">
            Netptune
          </span>
        </a>

        <nav class="hidden items-center gap-7 md:flex">
          <For each={navLinks}>
            {(link) => (
              <a
                href={link.href}
                target={link.target}
                rel={link.rel}
                class="text-sm text-slate-500 transition-colors hover:text-slate-900 dark:text-white/55 dark:hover:text-white"
              >
                {link.label}
              </a>
            )}
          </For>
        </nav>

        <div class="flex shrink-0 items-center gap-2">
          <ThemeToggle />
          <Button variant="outline" size="sm" href="https://github.com/JoelCrosby/netptune">
            <Star size={14} />
            <span class="hidden sm:block">Star on GitHub</span>
          </Button>
          <Button variant="primary" size="sm" href="https://netptune.co.uk/">
            Get started free
          </Button>
        </div>
      </div>
    </header>
  );
}
