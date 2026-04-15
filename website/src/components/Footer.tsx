import { For } from 'solid-js';
import { GitBranch } from 'lucide-solid';
import { footerColumns } from '~/data/footer';

export default function Footer() {
  return (
    <footer class="border-t border-slate-800 bg-dark px-6 pt-16 pb-10 dark:border-white/10 dark:bg-black">
      <div class="mx-auto max-w-6xl">
        <div class="mb-14 grid grid-cols-2 gap-10 sm:grid-cols-4">
          {/* Brand column */}
          <div class="col-span-2 sm:col-span-1">
            <a href="/" class="mb-4 flex items-center gap-2">
              <img
                class="flex h-7 w-7 items-center justify-center rounded-md bg-brand text-white"
                src="/netptune-logo.webp"
                alt="Netptune Icon"
              />
              <span class="text-[15px] font-semibold text-white">Netptune</span>
            </a>
            <p class="text-sm leading-relaxed text-slate-500 dark:text-white/35">
              Open source project management for teams who need real control over their tools and
              data.
            </p>
          </div>

          {/* Link columns */}
          <For each={footerColumns}>
            {(col) => (
              <div>
                <p class="mb-4 text-xs font-semibold tracking-wider text-slate-300 uppercase dark:text-white/60">
                  {col.heading}
                </p>
                <ul class="space-y-2.5">
                  <For each={col.links}>
                    {(link) => (
                      <li>
                        <a
                          href={link.href}
                          class="text-sm text-slate-500 transition-colors hover:text-slate-300 dark:text-white/35 dark:hover:text-white/70"
                        >
                          {link.label}
                        </a>
                      </li>
                    )}
                  </For>
                </ul>
              </div>
            )}
          </For>
        </div>

        <div class="flex flex-col items-center justify-between gap-4 border-t border-slate-800 pt-8 sm:flex-row dark:border-white/10">
          <p class="text-sm text-slate-600 dark:text-white/25">
            &copy; {new Date().getFullYear()} Netptune. MIT licensed.
          </p>
          <a
            href="https://github.com/JoelCrosby/netptune"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center gap-2 text-sm text-slate-500 transition-colors hover:text-slate-300 dark:text-white/35 dark:hover:text-white/70"
          >
            <GitBranch size={15} />
            JoelCrosby/netptune
          </a>
        </div>
      </div>
    </footer>
  );
}
