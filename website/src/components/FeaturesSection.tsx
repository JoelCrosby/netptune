import { For } from 'solid-js';
import Section from './Section';
import { features } from '~/data/features';

export default function FeaturesSection() {
  return (
    <Section id="features" class="bg-slate-50 dark:bg-black">
      <div class="mb-14 text-center">
        <p class="mb-3 text-sm font-semibold tracking-wider text-brand uppercase">Features</p>
        <h2 class="text-4xl font-bold tracking-tight text-slate-900 dark:text-white">
          Everything your team needs.
          <br />
          Nothing it doesn't.
        </h2>
      </div>

      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <For each={features}>
          {(f) => {
            const Icon = f.icon;
            return (
              <div class="group rounded-xl border border-slate-200 bg-white p-6 transition-all hover:border-violet-200 hover:shadow-sm dark:border-white/10 dark:bg-[#111] dark:hover:border-violet-500/30">
                <div class="mb-4 flex h-10 w-10 items-center justify-center rounded-lg bg-violet-50 text-brand dark:bg-violet-500/10">
                  <Icon size={20} />
                </div>
                <h3 class="mb-2 font-semibold text-slate-900 dark:text-white">{f.title}</h3>
                <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
                  {f.description}
                </p>
              </div>
            );
          }}
        </For>
      </div>
    </Section>
  );
}
