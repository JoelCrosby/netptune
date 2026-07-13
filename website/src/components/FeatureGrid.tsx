import { For } from 'solid-js';
import Section from './Section';
import { featureGridItems } from '~/data/feature-grid';

export default function FeatureGrid() {
  return (
    <Section class="bg-slate-50 dark:bg-black">
      <div class="mb-14 max-w-3xl">
        <p class="mb-3 font-mono text-sm font-semibold tracking-wider text-brand uppercase">
          Batteries included
        </p>
        <h2 class="text-4xl font-bold tracking-tight text-slate-900 dark:text-white">
          The project primitives are already there.
        </h2>
        <p class="mt-5 max-w-2xl text-lg leading-relaxed text-slate-500 dark:text-white/55">
          Start with a complete workflow instead of assembling another collection of integrations.
        </p>
      </div>

      <div class="grid gap-px overflow-hidden rounded-2xl border border-slate-200 bg-slate-200 shadow-sm sm:grid-cols-2 lg:grid-cols-3 dark:border-white/10 dark:bg-white/10">
        <For each={featureGridItems}>
          {(f) => {
            const Icon = f.icon;
            return (
              <div class="group flex items-start gap-4 bg-white px-6 py-6 transition-colors hover:bg-violet-50/50 dark:bg-[#0a0a0a] dark:hover:bg-white/[0.06]">
                <div class="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-violet-50 text-brand transition-transform duration-200 group-hover:scale-110 dark:bg-violet-500/10">
                  <Icon size={18} />
                </div>
                <div>
                  <p class="text-sm font-semibold text-slate-900 dark:text-white">{f.title}</p>
                  <p class="mt-0.5 text-sm text-slate-500 dark:text-white/55">{f.description}</p>
                </div>
              </div>
            );
          }}
        </For>
      </div>
    </Section>
  );
}
