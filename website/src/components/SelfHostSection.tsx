import { For } from 'solid-js';
import { GitBranch } from 'lucide-solid';
import Button from './Button';
import { selfHostStack } from '~/data/self-host-stack';

export default function SelfHostSection() {
  return (
    <section id="self-host" class="bg-dark px-6 py-20 dark:bg-black">
      <div class="mx-auto max-w-6xl">
        <div class="flex flex-col items-start gap-14 lg:flex-row lg:gap-20">
          {/* Text */}
          <div class="flex-1">
            <p class="mb-4 text-sm font-semibold tracking-wider text-brand uppercase">
              Self-hosting
            </p>
            <h2 class="mb-5 text-4xl leading-tight font-bold text-white">
              Your data. Your servers.
              <br />
              Your rules.
            </h2>
            <p class="mb-8 leading-relaxed text-slate-400 dark:text-white/55">
              Netptune is open source and built to be self-hosted. Deploy it on your own
              infrastructure in minutes with Docker or Kubernetes. No vendor lock-in, no per-seat
              pricing surprises, no wondering where your data lives.
            </p>

            <div class="flex flex-col gap-4 sm:flex-row">
              <Button variant="primary" size="md" href="https://github.com/JoelCrosby/netptune">
                <GitBranch size={15} />
                View on GitHub
              </Button>
              <Button
                variant="outline"
                size="md"
                href="#"
                class="border-slate-600 bg-transparent text-slate-300 hover:border-brand hover:text-white dark:border-white/20 dark:text-white/70"
              >
                Read the docs
              </Button>
            </div>
          </div>

          {/* Stack */}
          <div class="w-full flex-1">
            <p class="mb-4 text-xs font-semibold tracking-wider text-slate-500 uppercase dark:text-white/35">
              Runs on your stack
            </p>
            <div class="space-y-3">
              <For each={selfHostStack}>
                {(item) => {
                  const Icon = item.icon;
                  return (
                    <div class="flex items-center gap-4 rounded-xl border border-slate-700/50 bg-dark-surface p-4 transition-colors hover:border-slate-600 dark:border-white/10 dark:bg-white/5 dark:hover:border-white/20">
                      <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-700 text-slate-300 dark:bg-white/10 dark:text-white/60">
                        <Icon size={18} />
                      </div>
                      <div>
                        <p class="text-sm font-medium text-slate-200 dark:text-white/80">
                          {item.label}
                        </p>
                        <p class="text-xs text-slate-500 dark:text-white/35">{item.sublabel}</p>
                      </div>
                      <div class="ml-auto">
                        <div class="h-2 w-2 rounded-full bg-brand" />
                      </div>
                    </div>
                  );
                }}
              </For>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
