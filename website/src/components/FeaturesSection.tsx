import { For, Show, createSignal, onMount } from 'solid-js';
import { featureTabs } from '~/data/feature-tabs';

const tabInterval = 7000;

export default function FeaturesSection() {
  const [active, setActive] = createSignal(0);
  const [autoplay, setAutoplay] = createSignal(true);
  const [paused, setPaused] = createSignal(false);

  const tab = () => featureTabs[active()];

  onMount(() => {
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reducedMotion) {
      setAutoplay(false);
    }
  });

  function advance() {
    setActive((i) => (i + 1) % featureTabs.length);
  }

  function select(index: number) {
    setAutoplay(false);
    setActive(index);
  }

  function onTabKeyDown(event: KeyboardEvent) {
    const count = featureTabs.length;
    const targets: Record<string, number> = {
      ArrowRight: (active() + 1) % count,
      ArrowLeft: (active() - 1 + count) % count,
      Home: 0,
      End: count - 1,
    };
    const target = targets[event.key];

    if (target === undefined) return;

    event.preventDefault();
    select(target);
    document.getElementById(`feature-tab-${target}`)?.focus();
  }

  return (
    <section
      id="features"
      class="bg-mist px-6 pt-30 pb-27.5"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusIn={() => setPaused(true)}
      onFocusOut={() => setPaused(false)}
    >
      <div class="mx-auto flex max-w-310 flex-col gap-12">
        <div class="grid items-end gap-x-16 gap-y-6 lg:grid-cols-2">
          <div class="flex flex-col gap-4.5">
            <span class="font-geist-mono text-xs tracking-[0.08em] text-brand uppercase">
              01 — One plan, every angle
            </span>
            <h2 class="m-0 text-[clamp(38px,4.6vw,64px)] leading-none font-bold tracking-[-0.04em] text-balance">
              Flip views without rebuilding the plan.
            </h2>
          </div>
          <p class="m-0 max-w-130 text-lg leading-[1.6] text-pretty text-ink-muted">
            Boards, a calendar, a roadmap, sprints and pages all read from the same work. Change it
            in one place and everyone sees it straight away.
          </p>
        </div>

        <div
          role="tablist"
          aria-label="Views"
          class="flex max-w-full flex-wrap gap-2 self-start rounded-3xl bg-[#efebf6] p-1.5 sm:rounded-full"
          onKeyDown={onTabKeyDown}
        >
          <For each={featureTabs}>
            {(t, i) => (
              <button
                type="button"
                role="tab"
                id={`feature-tab-${i()}`}
                aria-selected={i() === active()}
                aria-controls="feature-tab-panel"
                tabIndex={i() === active() ? 0 : -1}
                class="cursor-pointer rounded-full px-4.5 py-2.5 text-sm font-medium transition-colors"
                classList={{
                  'bg-ink text-white shadow-[0_6px_20px_rgb(20_15_34/0.25)]': i() === active(),
                  'text-ink-muted hover:text-ink': i() !== active(),
                }}
                onClick={() => select(i())}
              >
                {t.label}
              </button>
            )}
          </For>
        </div>

        <div
          id="feature-tab-panel"
          role="tabpanel"
          aria-labelledby={`feature-tab-${active()}`}
          class="grid items-center gap-10 lg:grid-cols-3"
        >
          <div class="flex max-w-105 flex-col gap-4.5">
            <span class="font-geist-mono text-xs text-violet-500">{tab().eyebrow}</span>
            <h3 class="m-0 text-[32px] leading-[1.1] font-[650] tracking-[-0.03em] text-balance">
              {tab().title}
            </h3>
            <p class="m-0 text-base leading-[1.65] text-pretty text-ink-muted">
              {tab().description}
            </p>
            <p class="m-0 border-t border-hairline pt-4 text-sm leading-[1.6] text-pretty text-ink-subtle">
              {tab().extra}
            </p>
            <div class="mt-2 h-0.75 overflow-hidden rounded-full bg-hairline">
              <Show
                when={autoplay() && tab()}
                keyed
                fallback={<div class="h-full w-full bg-brand" />}
              >
                <div
                  class="h-full w-0 animate-[feature-tab-progress_var(--tab-interval)_linear_forwards] bg-linear-90 from-brand to-fuchsia-500"
                  classList={{ '[animation-play-state:paused]': paused() }}
                  style={{ '--tab-interval': `${tabInterval}ms` }}
                  onAnimationEnd={advance}
                />
              </Show>
            </div>
          </div>

          <div class="relative min-w-0 lg:col-span-2">
            <div class="pointer-events-none absolute -inset-y-10 -right-5 left-[10%] bg-[radial-gradient(ellipse_at_60%_50%,rgb(139_92_246/0.28),rgb(217_70_239/0.12)_45%,transparent_70%)] blur-[20px]" />
            <div class="relative rounded-[20px] bg-linear-140 from-brand/35 via-fuchsia-300/25 to-sky-300/30 p-2.5 shadow-[0_40px_80px_-30px_rgb(80_40_160/0.45)]">
              <div class="relative aspect-video overflow-hidden rounded-xl bg-white">
                <For each={featureTabs}>
                  {(t, i) => (
                    <img
                      src={t.image}
                      alt={t.title}
                      width={t.width}
                      height={t.height}
                      loading="lazy"
                      aria-hidden={i() !== active()}
                      class="absolute inset-0 size-full object-cover [object-position:top_left] transition-opacity duration-500"
                      classList={{ 'opacity-0': i() !== active() }}
                    />
                  )}
                </For>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
