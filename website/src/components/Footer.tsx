import { For } from 'solid-js';
import { footerColumns } from '~/data/footer';

export default function Footer() {
  return (
    <footer class="border-t border-purple-200/10 bg-night px-6 pt-16 pb-10 font-geist text-white">
      <div class="mx-auto flex max-w-310 flex-col gap-14">
        <div class="grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div class="flex flex-col gap-3.5">
            <a href="/" class="flex items-center gap-2.5">
              <img src="/favicon.svg" alt="" class="size-7 rounded-[7px]" />
              <span class="text-[17px] font-semibold text-white">Netptune</span>
            </a>
            <p class="m-0 max-w-60 text-sm leading-[1.55] text-white/50">
              The project workspace built for developers.
            </p>
          </div>

          <For each={footerColumns}>
            {(col) => (
              <div class="flex flex-col gap-3">
                <p class="m-0 font-geist-mono text-[11px] tracking-[0.08em] text-white/40 uppercase">
                  {col.heading}
                </p>
                <For each={col.links}>
                  {(link) => (
                    <a
                      href={link.href}
                      class="text-sm text-white/70 transition-colors hover:text-white"
                    >
                      {link.label}
                    </a>
                  )}
                </For>
              </div>
            )}
          </For>
        </div>

        <div class="flex flex-wrap justify-between gap-3 border-t border-purple-200/8 pt-6 text-[13px] text-white/40">
          <span>&copy; {new Date().getFullYear()} Netptune</span>
          <span class="font-geist-mono">netptune.co.uk</span>
        </div>
      </div>
    </footer>
  );
}
