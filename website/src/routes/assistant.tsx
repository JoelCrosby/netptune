import { For, createSignal, onCleanup, onMount, type Accessor } from 'solid-js';
import Nav from '~/components/Nav';
import Footer from '~/components/Footer';
import {
  assistantHelpsWith,
  assistantPrompts,
  assistantTrust,
  assistantVerbs,
  type AssistantVerbLabel,
} from '~/data/assistant-page';

const typeInterval = 45;
const cycleDelay = 2600;

const helpsWithStyles = [
  'border-ink bg-ink text-white',
  'border-[#e2dbee] bg-white text-ink',
  'border-[#ede5fa] bg-[#ede5fa] text-[#4a2a8a]',
  'border-[#e2dbee] bg-white text-ink',
  'border-transparent bg-linear-120 from-brand to-fuchsia-600 text-white',
];

function verbIndex(label: AssistantVerbLabel): number {
  return assistantVerbs.findIndex((verb) => verb.label === label);
}

interface PromptShowcase {
  prompt: Accessor<number>;
  typed: Accessor<string>;
  verb: Accessor<number>;
  pickPrompt: (index: number) => void;
  pickVerb: (index: number) => void;
  stopCycling: () => void;
}

function createPromptShowcase(): PromptShowcase {
  const [prompt, setPrompt] = createSignal(0);
  const [typed, setTyped] = createSignal(assistantPrompts[0].text);
  const [verb, setVerb] = createSignal(verbIndex(assistantPrompts[0].verb));

  let typing: ReturnType<typeof setInterval> | undefined;
  let cycle: ReturnType<typeof setTimeout> | undefined;
  let cycling = true;
  let animate = true;

  function stopTimers() {
    clearInterval(typing);
    clearTimeout(cycle);
  }

  function showPrompt(index: number) {
    stopTimers();

    const { text, verb } = assistantPrompts[index];

    setPrompt(index);
    setVerb(verbIndex(verb));

    if (!animate) {
      setTyped(text);
      return;
    }

    let length = 0;
    setTyped('');

    typing = setInterval(() => {
      length++;
      setTyped(text.slice(0, length));

      if (length < text.length) return;

      clearInterval(typing);

      if (cycling) {
        cycle = setTimeout(() => showPrompt((index + 1) % assistantPrompts.length), cycleDelay);
      }
    }, typeInterval);
  }

  function stopCycling() {
    cycling = false;
    clearTimeout(cycle);
  }

  function pickPrompt(index: number) {
    cycling = false;
    showPrompt(index);
  }

  function pickVerb(index: number) {
    stopCycling();
    setVerb(index);
  }

  onMount(() => {
    animate = !window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (animate) {
      showPrompt(0);
    }
  });

  onCleanup(stopTimers);

  return { prompt, typed, verb, pickPrompt, pickVerb, stopCycling };
}

function AssistantHero(props: { showcase: PromptShowcase }) {
  return (
    <section class="assistant-hero relative overflow-hidden px-6 text-white">
      <div class="home-hero-stars" />
      <div class="assistant-hero-horizon" />
      <div class="assistant-hero-ring" />

      <div class="relative mx-auto flex max-w-250 flex-col items-center gap-7 pt-24 pb-50 text-center">
        <span class="inline-flex items-center gap-2.5 rounded-full border border-purple-200/16 bg-purple-200/7 py-1.25 pr-3.5 pl-1.25 text-[13px] whitespace-nowrap text-white/80">
          <span class="flex size-6 items-center justify-center rounded-full bg-linear-135 from-violet-500 to-fuchsia-500 text-xs font-bold text-white">
            ✦
          </span>
          The Netptune assistant
        </span>

        <h1 class="m-0 text-[clamp(48px,7vw,100px)] leading-[0.95] font-bold tracking-[-0.05em] text-balance">
          Knows your work.{' '}
          <span class="bg-[linear-gradient(100deg,#c4b5fd_0%,#a78bfa_30%,#e879f9_70%,#f0abfc_100%)] bg-clip-text text-transparent">
            Asks before it touches anything.
          </span>
        </h1>

        <p class="m-0 max-w-155 text-[19px] leading-[1.55] text-pretty text-white/66">
          Ask it questions, have it draft changes, tidy up the backlog or write the boring bits. It
          does the legwork and you keep the final say.
        </p>

        <div class="mt-3 flex w-full max-w-180 flex-col gap-3.5">
          <div class="flex items-center gap-3.5 rounded-full border border-purple-200/22 bg-[rgb(22_14_40/0.78)] py-2.5 pr-2.5 pl-5.5 text-left shadow-[0_20px_60px_rgb(0_0_0/0.45),0_0_0_6px_rgb(139_92_246/0.08)] backdrop-blur-[10px]">
            <span class="shrink-0 text-base text-fuchsia-300" aria-hidden="true">
              ✦
            </span>
            <span
              class="min-w-0 flex-1 truncate text-base text-white sm:text-lg"
              aria-hidden="true"
            >
              {props.showcase.typed()}
              <span class="ml-0.5 inline-block h-[1.05em] w-0.5 animate-[prompt-caret_1s_step-end_infinite] bg-fuchsia-300 align-[-2px] motion-reduce:animate-none" />
            </span>
            <a
              href="#capabilities"
              class="inline-flex shrink-0 items-center gap-2 rounded-full bg-white px-5 py-3 text-[15px] font-semibold whitespace-nowrap text-ink transition-colors hover:bg-purple-100"
              onClick={() => props.showcase.stopCycling()}
            >
              See how →
            </a>
          </div>

          <div class="flex flex-wrap justify-center gap-2">
            <For each={assistantPrompts}>
              {(prompt, i) => (
                <button
                  type="button"
                  aria-pressed={i() === props.showcase.prompt()}
                  class="cursor-pointer rounded-full border px-3.5 py-2 text-sm transition-colors"
                  classList={{
                    'border-fuchsia-300/50 bg-purple-200/16 text-white':
                      i() === props.showcase.prompt(),
                    'border-purple-200/14 bg-white/4 text-white/62 hover:text-white':
                      i() !== props.showcase.prompt(),
                  }}
                  onClick={() => props.showcase.pickPrompt(i())}
                >
                  “{prompt.text}”
                </button>
              )}
            </For>
          </div>
        </div>
      </div>
    </section>
  );
}

function CapabilitiesSection(props: { showcase: PromptShowcase }) {
  const active = () => assistantVerbs[props.showcase.verb()];

  let section!: HTMLElement;

  onMount(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        const inView = entries.some((entry) => entry.isIntersecting);

        if (!inView) return;

        props.showcase.stopCycling();
        observer.disconnect();
      },
      { threshold: 0.2 },
    );

    observer.observe(section);
    onCleanup(() => observer.disconnect());
  });

  function onVerbKeyDown(event: KeyboardEvent) {
    const count = assistantVerbs.length;
    const next = (props.showcase.verb() + 1) % count;
    const previous = (props.showcase.verb() - 1 + count) % count;
    const targets: Record<string, number> = {
      ArrowDown: next,
      ArrowRight: next,
      ArrowUp: previous,
      ArrowLeft: previous,
      Home: 0,
      End: count - 1,
    };
    const target = targets[event.key];

    if (target === undefined) return;

    event.preventDefault();
    props.showcase.pickVerb(target);
    document.getElementById(`assistant-verb-${target}`)?.focus();
  }

  return (
    <section
      ref={section}
      id="capabilities"
      class="scroll-mt-16 bg-mist px-6 py-30"
      onFocusIn={() => props.showcase.stopCycling()}
    >
      <div class="mx-auto flex max-w-310 flex-col gap-14">
        <div class="grid items-end gap-x-16 gap-y-6 lg:grid-cols-2">
          <div class="flex flex-col gap-4.5">
            <span class="font-geist-mono text-xs tracking-[0.08em] text-brand uppercase">
              01 — {assistantVerbs.length} ways it helps
            </span>
            <h2 class="m-0 text-[clamp(38px,4.6vw,64px)] leading-none font-bold tracking-[-0.04em] text-balance">
              Does the legwork. You keep the final say.
            </h2>
          </div>
          <p class="m-0 max-w-130 text-lg leading-[1.6] text-pretty text-ink-muted">
            Pick a verb to see it in action.
          </p>
        </div>

        <div class="flex flex-wrap items-start gap-12">
          <div
            role="tablist"
            aria-label="What the assistant does"
            aria-orientation="vertical"
            class="flex max-w-85 flex-[1_1_260px] flex-col"
            onKeyDown={onVerbKeyDown}
          >
            <For each={assistantVerbs}>
              {(verb, i) => (
                <button
                  type="button"
                  role="tab"
                  id={`assistant-verb-${i()}`}
                  aria-selected={i() === props.showcase.verb()}
                  aria-controls="assistant-verb-panel"
                  tabIndex={i() === props.showcase.verb() ? 0 : -1}
                  class="group flex cursor-pointer items-baseline gap-4 py-2.5 text-left"
                  onClick={() => props.showcase.pickVerb(i())}
                >
                  <span
                    class="w-5.5 shrink-0 font-geist-mono text-xs transition-colors"
                    classList={{
                      'text-fuchsia-500': i() === props.showcase.verb(),
                      'text-[#c9c1d8]': i() !== props.showcase.verb(),
                    }}
                  >
                    {String(i() + 1).padStart(2, '0')}
                  </span>
                  <span
                    class="text-[clamp(40px,4.4vw,64px)] leading-none font-bold tracking-[-0.045em] transition-colors duration-200"
                    classList={{
                      'text-ink': i() === props.showcase.verb(),
                      'text-[#d6cfe3] group-hover:text-[#b3a8c7]': i() !== props.showcase.verb(),
                    }}
                  >
                    {verb.label}
                  </span>
                </button>
              )}
            </For>
          </div>

          <div
            id="assistant-verb-panel"
            role="tabpanel"
            aria-labelledby={`assistant-verb-${props.showcase.verb()}`}
            class="flex min-w-0 flex-[999_1_520px] flex-col gap-7"
          >
            <div class="relative">
              <div class="pointer-events-none absolute -inset-y-10 -right-5 left-[10%] bg-[radial-gradient(ellipse_at_60%_50%,rgb(139_92_246/0.28),rgb(217_70_239/0.12)_45%,transparent_70%)] blur-[20px]" />
              <div class="relative rounded-[20px] bg-linear-140 from-brand/35 via-fuchsia-300/25 to-sky-300/30 p-2.5 shadow-[0_40px_80px_-30px_rgb(80_40_160/0.45)]">
                <div class="relative aspect-video overflow-hidden rounded-xl bg-white">
                  <For each={assistantVerbs}>
                    {(verb, i) => (
                      <img
                        src={verb.image}
                        alt={verb.alt}
                        width={1600}
                        height={900}
                        loading="lazy"
                        aria-hidden={i() !== props.showcase.verb()}
                        class="absolute inset-0 size-full object-cover [object-position:top_left] transition-opacity duration-500"
                        classList={{ 'opacity-0': i() !== props.showcase.verb() }}
                      />
                    )}
                  </For>
                </div>
              </div>
            </div>

            <div class="grid items-start gap-x-10 gap-y-4 md:grid-cols-2">
              <h3 class="m-0 text-[30px] leading-[1.1] font-[650] tracking-[-0.03em] text-balance">
                {active().title}
              </h3>
              <div class="flex flex-col gap-3.5">
                <p class="m-0 text-base leading-[1.65] text-pretty text-ink-muted">
                  {active().description}
                </p>
                <a
                  href={active().link.href}
                  class="self-start border-b-[1.5px] border-brand/30 pb-0.5 text-[15px] font-semibold whitespace-nowrap text-brand transition-colors hover:border-brand"
                >
                  {active().link.label} →
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function TrustSection() {
  return (
    <section class="relative overflow-hidden bg-[#0d0819] bg-[image:radial-gradient(ellipse_50%_60%_at_85%_20%,rgb(217_70_239/0.2),transparent_70%),radial-gradient(ellipse_60%_70%_at_10%_90%,rgb(103_58_183/0.45),transparent_70%)] px-6 py-30 text-white">
      <div class="relative mx-auto flex max-w-310 flex-col gap-16">
        <div class="grid items-end gap-x-16 gap-y-6 lg:grid-cols-2">
          <div class="flex flex-col gap-4.5">
            <span class="font-geist-mono text-xs tracking-[0.08em] text-fuchsia-300 uppercase">
              02 — Peace of mind
            </span>
            <h2 class="m-0 text-[clamp(44px,5.6vw,84px)] leading-[0.95] font-bold tracking-[-0.05em]">
              Helpful, not nosy.
            </h2>
          </div>
          <a
            href="/docs/assistant/access-and-cost"
            class="self-start justify-self-start border-b-[1.5px] border-fuchsia-300/50 pb-0.5 text-base font-medium text-white transition-colors hover:text-fuchsia-300 lg:self-end"
          >
            Read about access, privacy and cost →
          </a>
        </div>

        <div class="grid border-t border-purple-200/16 sm:grid-cols-2 lg:grid-cols-4">
          <For each={assistantTrust}>
            {(item, i) => (
              <div class="flex flex-col gap-3.5 pt-8 pr-7 pb-2">
                <span class="font-geist-mono text-xs text-fuchsia-300/80">
                  {String(i() + 1).padStart(2, '0')}
                </span>
                <span class="text-2xl leading-[1.15] font-[650] tracking-[-0.025em] text-balance">
                  {item.title}
                </span>
                <span class="text-[15px] leading-[1.55] text-pretty text-white/60">
                  {item.description}
                </span>
              </div>
            )}
          </For>
        </div>
      </div>
    </section>
  );
}

function HelpsWithSection() {
  return (
    <section class="bg-mist px-6 py-30">
      <div class="mx-auto flex max-w-250 flex-col items-center gap-5 text-center">
        <span class="font-geist-mono text-xs tracking-[0.08em] whitespace-nowrap text-brand uppercase">
          03 — Reach
        </span>
        <h2 class="m-0 text-[clamp(38px,4.6vw,64px)] leading-none font-bold tracking-[-0.04em] text-balance">
          It can lend a hand with…
        </h2>
        <p class="m-0 text-lg leading-[1.6] text-ink-muted">
          Pretty much everything in your workspace, as far as your role allows.
        </p>
        <div class="mt-6 flex flex-wrap justify-center gap-3">
          <For each={assistantHelpsWith}>
            {(item, i) => (
              <span
                class={`rounded-full border px-6.5 py-3.5 text-[clamp(18px,2vw,24px)] font-semibold tracking-[-0.02em] whitespace-nowrap ${helpsWithStyles[i() % helpsWithStyles.length]}`}
              >
                {item}
              </span>
            )}
          </For>
        </div>
      </div>
    </section>
  );
}

function AssistantCta() {
  return (
    <section class="assistant-cta relative overflow-hidden px-6 py-32.5 text-center text-white">
      <div class="assistant-cta-ring" />
      <div class="relative mx-auto flex max-w-190 flex-col items-center gap-6">
        <h2 class="m-0 text-[clamp(44px,6vw,88px)] leading-[0.95] font-bold tracking-[-0.05em]">
          Give it something to do.
        </h2>
        <p class="m-0 max-w-130 text-[19px] leading-[1.55] text-white/62">
          Create a workspace, ask your first question, and see what it would change before anything
          does.
        </p>
        <div class="flex flex-wrap justify-center gap-3 pt-2">
          <a
            href="https://app.netptune.co.uk/auth/register"
            class="rounded-full bg-white px-6.5 py-3.75 text-base font-semibold text-ink transition-colors hover:bg-purple-100"
          >
            Try it in Netptune →
          </a>
          <a
            href="/docs/assistant"
            class="rounded-full border border-purple-200/28 px-6.5 py-3.75 text-base font-medium text-white transition-colors hover:bg-purple-200/8"
          >
            Read the guide
          </a>
        </div>
      </div>
    </section>
  );
}

export default function AssistantPage() {
  const showcase = createPromptShowcase();

  return (
    <div class="bg-mist text-ink antialiased">
      <Nav />
      <main>
        <AssistantHero showcase={showcase} />
        <CapabilitiesSection showcase={showcase} />
        <TrustSection />
        <HelpsWithSection />
        <AssistantCta />
      </main>
      <Footer />
    </div>
  );
}
