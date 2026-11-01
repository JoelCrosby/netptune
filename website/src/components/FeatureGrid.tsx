import { For, createSignal, createUniqueId } from 'solid-js';
import { featureGroups, type FeatureGroup } from '~/data/feature-groups';

const pillClasses = {
  dark: {
    on: 'border-transparent bg-white text-ink',
    off: 'border-white/18 bg-white/8 text-white',
  },
  light: {
    on: 'border-transparent bg-ink text-white',
    off: 'border-ink/12 bg-ink/4 text-ink',
  },
};

function FeatureGroupCard(props: { group: FeatureGroup }) {
  const [selected, setSelected] = createSignal(0);
  const captionId = createUniqueId();

  const caption = () => props.group.items[selected()].description;
  const pills = () => pillClasses[props.group.tone];

  return (
    <div
      class={`relative flex min-h-75 flex-col gap-5.5 overflow-hidden rounded-3xl p-7.5 ${props.group.background}`}
      classList={{
        'text-white': props.group.tone === 'dark',
        'text-ink': props.group.tone === 'light',
        'md:col-span-2': props.group.wide,
      }}
    >
      <div
        class={`pointer-events-none absolute -top-15 -right-15 size-55 rounded-full ${props.group.orb}`}
      />
      <div class="relative flex items-baseline justify-between gap-3">
        <h3 class="m-0 text-[clamp(30px,3.2vw,42px)] leading-none font-bold tracking-[-0.04em]">
          {props.group.name}
        </h3>
        <span class="font-geist-mono text-xs opacity-60">
          {String(props.group.items.length).padStart(2, '0')}
        </span>
      </div>

      <div class="relative flex flex-wrap gap-2">
        <For each={props.group.items}>
          {(item, i) => (
            <button
              type="button"
              aria-controls={captionId}
              class={`cursor-default rounded-full border px-4 py-2.25 text-[15px] font-medium transition-colors ${i() === selected() ? pills().on : pills().off}`}
              onMouseEnter={() => setSelected(i())}
              onFocus={() => setSelected(i())}
              onClick={() => setSelected(i())}
            >
              {item.title}
            </button>
          )}
        </For>
      </div>

      <p
        id={captionId}
        aria-live="polite"
        class="relative mt-auto mb-0 min-h-12 text-base leading-normal text-pretty opacity-80"
      >
        {caption()}
      </p>
    </div>
  );
}

export default function FeatureGrid() {
  return (
    <section class="border-t border-hairline bg-[#f1edf7] px-6 py-27.5">
      <div class="mx-auto flex max-w-310 flex-col gap-12">
        <div class="grid items-end gap-x-16 gap-y-6 lg:grid-cols-2">
          <div class="flex flex-col gap-4.5">
            <span class="font-geist-mono text-xs tracking-[0.08em] text-brand uppercase">
              04 — Defaults included
            </span>
            <h2 class="m-0 text-[clamp(38px,4.6vw,64px)] leading-none font-bold tracking-[-0.04em] text-balance">
              Useful on day one. No plugin shopping.
            </h2>
          </div>
          <p class="m-0 max-w-130 text-lg leading-[1.6] text-ink-muted">
            Everything here ships in every workspace. Hover anything to see what it does.
          </p>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <For each={featureGroups}>{(group) => <FeatureGroupCard group={group} />}</For>
        </div>
      </div>
    </section>
  );
}
