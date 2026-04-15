import { For } from 'solid-js';
import Section from './Section';
import { comparisons } from '~/data/comparisons';

export default function ComparisonSection() {
  return (
    <Section class="bg-slate-50 dark:bg-black">
      <div class="mb-14 text-center">
        <p class="mb-3 text-sm font-semibold tracking-wider text-brand uppercase">Comparison</p>
        <h2 class="text-4xl font-bold tracking-tight text-slate-900 dark:text-white">
          A better fit than the tools you've outgrown.
        </h2>
      </div>

      <div class="grid gap-6 sm:grid-cols-2">
        <For each={comparisons}>
          {(c) => (
            <div class="rounded-xl border border-slate-200 bg-white p-6 transition-all hover:border-violet-200 hover:shadow-sm dark:border-white/10 dark:bg-[#111] dark:hover:border-violet-500/30">
              <div class="mb-4 inline-flex items-center rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600 dark:bg-white/10 dark:text-white/60">
                {c.competitor}
              </div>
              <h3 class="mb-2 text-lg font-bold text-slate-900 dark:text-white">{c.headline}</h3>
              <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
                {c.description}
              </p>
            </div>
          )}
        </For>
      </div>
    </Section>
  );
}
