import { For } from 'solid-js';
import { ArrowRight, Sparkles } from 'lucide-solid';
import Button from './Button';
import { assistantBandItems } from '~/data/assistant-band';

export default function AssistantBand() {
  return (
    <section id="assistant" class="relative overflow-hidden bg-dark px-6 py-20 dark:bg-black">
      <div class="absolute inset-0 bg-[radial-gradient(ellipse_60%_60%_at_50%_0%,rgba(103,58,183,0.35),transparent)]" />

      <div class="relative mx-auto max-w-6xl">
        <div class="mb-12 flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div class="max-w-2xl">
            <p class="mb-3 inline-flex items-center gap-2 font-mono text-sm font-semibold tracking-wider text-violet-300 uppercase">
              <Sparkles size={14} />
              Meet the assistant
            </p>
            <h2 class="mb-4 text-4xl leading-tight font-bold tracking-tight text-white">
              A helping hand that asks first.
            </h2>
            <p class="text-lg leading-relaxed text-slate-400 dark:text-white/55">
              It knows your tasks, sprints, pages and code. Ask it anything, hand it the busywork,
              and keep the final say on every change.
            </p>
          </div>
          <Button variant="primary" size="lg" href="/assistant" class="shrink-0 self-start lg:self-auto">
            Explore the assistant
            <ArrowRight size={16} />
          </Button>
        </div>

        <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          <For each={assistantBandItems}>
            {(item) => {
              const Icon = item.icon;
              return (
                <a
                  href={item.href}
                  class="group rounded-2xl border border-white/10 bg-white/[0.04] p-6 transition-all duration-300 hover:-translate-y-1 hover:border-violet-400/40 hover:bg-white/[0.07]"
                >
                  <div class="mb-4 flex h-10 w-10 items-center justify-center rounded-xl bg-brand/20 text-violet-300 ring-1 ring-white/10">
                    <Icon size={18} />
                  </div>
                  <h3 class="mb-2 font-semibold text-white">{item.title}</h3>
                  <p class="text-sm leading-relaxed text-slate-400 dark:text-white/55">
                    {item.description}
                  </p>
                </a>
              );
            }}
          </For>
        </div>
      </div>
    </section>
  );
}
