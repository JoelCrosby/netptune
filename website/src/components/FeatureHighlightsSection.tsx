import { For, Match, Switch } from 'solid-js';
import Section from './Section';
import { featureHighlights } from '~/data/feature-highlights';

export default function FeatureHighlightsSection() {
  return (
    <Section class="bg-white dark:bg-black">
      <div class="space-y-24">
        <For each={featureHighlights}>
          {(h) => (
            <div
              class={`flex flex-col ${h.reversed ? 'lg:flex-row-reverse' : 'lg:flex-row'} items-center gap-12 lg:gap-16`}
            >
              <div class="min-w-0 flex-1">
                <p class="mb-3 text-sm font-semibold tracking-wider text-brand uppercase">
                  {h.eyebrow}
                </p>
                <h2 class="mb-4 text-3xl leading-tight font-bold text-slate-900 lg:text-4xl dark:text-white">
                  {h.title}
                </h2>
                <p class="leading-relaxed text-slate-500 dark:text-white/55">{h.description}</p>
                {h.extra && <div class="mt-5">{h.extra()}</div>}
              </div>

              <Switch>
                <Match when={typeof h.visual === 'string'}>
                  <img
                    src={typeof h.visual === 'string' ? h.visual : undefined}
                    alt={h.title}
                    class="w-full max-w-md flex-1 rounded-lg border border-black/15 shadow-lg shadow-brand/10 dark:border-white/15"
                  />
                </Match>
                <Match when={typeof h.visual !== 'string'}>
                  {typeof h.visual !== 'string' && h.visual()}
                </Match>
              </Switch>
            </div>
          )}
        </For>
      </div>
    </Section>
  );
}
