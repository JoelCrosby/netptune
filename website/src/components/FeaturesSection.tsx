import { For } from 'solid-js';
import Section from './Section';
import { features } from '~/data/features';

export default function FeaturesSection() {
  return (
    <Section id="features" class="bg-slate-50 dark:bg-black">
      <div class="mb-14 max-w-3xl">
        <p class="mb-3 font-mono text-sm font-semibold tracking-wider text-brand uppercase">
          Developer experience
        </p>
        <h2 class="text-4xl font-bold tracking-tight text-slate-900 dark:text-white">
          The structure software teams need.
          <br />
          None of the process theatre.
        </h2>
        <p class="mt-5 max-w-2xl text-lg leading-relaxed text-slate-500 dark:text-white/55">
          Netptune keeps planning close to the work: fast boards, explicit ownership, searchable
          history, and automation that stays understandable.
        </p>
      </div>

      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <For each={features}>
          {(f) => {
            const Icon = f.icon;
            return (
              <div class="group relative overflow-hidden rounded-2xl border border-slate-200/80 bg-gradient-to-b from-white to-slate-50/70 p-7 transition-all duration-300 hover:-translate-y-1 hover:border-violet-300 hover:shadow-xl hover:shadow-brand/10 dark:border-white/10 dark:from-white/[0.07] dark:to-white/[0.025] dark:hover:border-violet-500/40">
                <div class="absolute -top-16 -right-16 h-32 w-32 rounded-full bg-brand/0 blur-3xl transition-colors duration-300 group-hover:bg-brand/15" />
                <div class="relative mb-5 flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-violet-100 to-fuchsia-50 text-brand ring-1 ring-brand/10 dark:from-violet-500/20 dark:to-fuchsia-500/10 dark:ring-white/10">
                  <Icon size={20} />
                </div>
                <h3 class="relative mb-2 text-lg font-semibold text-slate-900 dark:text-white">
                  {f.title}
                </h3>
                <p class="relative text-sm leading-relaxed text-slate-500 dark:text-white/55">
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
