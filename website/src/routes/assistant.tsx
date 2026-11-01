import { For } from 'solid-js';
import Nav from '~/components/Nav';
import Footer from '~/components/Footer';
import CtaSection from '~/components/CtaSection';
import {
  assistantHelpsWith,
  assistantPrompts,
  assistantShowcase,
  assistantTrust,
  type AssistantShowcaseItem,
} from '~/data/assistant-page';

function AssistantHero() {
  return (
    <section class="home-hero relative overflow-hidden px-6 text-white">
      <div class="home-hero-stars" />
      <div class="home-hero-grid" />

      <div class="relative mx-auto flex max-w-310 flex-col items-center gap-7 pt-24 pb-28 text-center">
        <span class="rounded-full border border-purple-200/16 bg-purple-200/7 px-3.5 py-1.5 font-geist-mono text-xs tracking-[0.08em] text-purple-200 uppercase">
          ✦ The Netptune assistant
        </span>

        <h1 class="m-0 max-w-230 text-[clamp(44px,6vw,84px)] leading-[0.98] font-bold tracking-[-0.045em] text-balance">
          Knows your work.{' '}
          <span class="bg-[linear-gradient(100deg,#c4b5fd_0%,#a78bfa_30%,#e879f9_70%,#f0abfc_100%)] bg-clip-text text-transparent">
            Asks before it touches anything.
          </span>
        </h1>

        <p class="m-0 max-w-150 text-[19px] leading-[1.55] text-pretty text-white/66">
          Ask it questions, have it draft changes, tidy up the backlog or write the boring bits. It
          does the legwork and you keep the final say.
        </p>

        <div class="flex flex-wrap justify-center gap-3">
          <a
            href="https://app.netptune.co.uk/auth/register"
            class="inline-flex items-center gap-2.5 rounded-full bg-white px-6 py-3.75 text-base font-semibold text-ink shadow-[0_10px_40px_rgb(167_139_250/0.35)] transition-colors hover:bg-purple-100"
          >
            Try it in Netptune <span>→</span>
          </a>
          <a
            href="/docs/assistant"
            class="inline-flex items-center gap-2.5 rounded-full border border-purple-200/28 px-6 py-3.75 text-base font-medium text-white transition-colors hover:bg-purple-200/8"
          >
            Read the guide
          </a>
        </div>

        <div class="flex max-w-215 flex-col items-center gap-4 pt-10">
          <span class="font-geist-mono text-xs text-white/45">Things people ask it</span>
          <div class="flex flex-wrap justify-center gap-2.5">
            <For each={assistantPrompts}>
              {(prompt) => (
                <span class="rounded-full border border-purple-200/14 bg-white/5 px-4 py-2 text-sm text-white/75">
                  “{prompt}”
                </span>
              )}
            </For>
          </div>
        </div>
      </div>
    </section>
  );
}

function ShowcaseRow(props: { item: AssistantShowcaseItem; index: number }) {
  return (
    <div class="grid items-center gap-10 lg:grid-cols-5 lg:gap-16">
      <div
        class="flex flex-col gap-4.5 lg:col-span-2"
        classList={{ 'lg:order-last': props.index % 2 === 1 }}
      >
        <span class="font-geist-mono text-xs tracking-[0.08em] text-brand uppercase">
          {String(props.index + 1).padStart(2, '0')} — {props.item.eyebrow}
        </span>
        <h2 class="m-0 text-[clamp(30px,3.4vw,44px)] leading-[1.05] font-bold tracking-[-0.035em] text-balance">
          {props.item.title}
        </h2>
        <p class="m-0 text-base leading-[1.65] text-pretty text-ink-muted">
          {props.item.description}
        </p>
        <a
          href={props.item.link.href}
          class="self-start text-sm font-semibold text-brand transition-colors hover:text-fuchsia-600"
        >
          {props.item.link.label} →
        </a>
      </div>

      <div class="relative min-w-0 lg:col-span-3">
        <div class="pointer-events-none absolute -inset-x-5 -inset-y-10 bg-[radial-gradient(ellipse_at_50%_50%,rgb(139_92_246/0.22),rgb(217_70_239/0.1)_45%,transparent_70%)] blur-[20px]" />
        <div class="relative rounded-[20px] bg-linear-140 from-brand/35 via-fuchsia-300/25 to-sky-300/30 p-2.5 shadow-[0_40px_80px_-30px_rgb(80_40_160/0.45)]">
          <img
            src={props.item.image}
            alt={props.item.alt}
            width={1600}
            height={900}
            loading="lazy"
            class="block w-full rounded-xl bg-white"
          />
        </div>
      </div>
    </div>
  );
}

function ShowcaseSection() {
  return (
    <section class="bg-mist px-6 py-30">
      <div class="mx-auto flex max-w-310 flex-col gap-28">
        <For each={assistantShowcase}>{(item, i) => <ShowcaseRow item={item} index={i()} />}</For>
      </div>
    </section>
  );
}

function TrustSection() {
  return (
    <section class="relative overflow-hidden bg-[#0d0819] bg-[image:radial-gradient(ellipse_50%_60%_at_85%_20%,rgb(217_70_239/0.22),transparent_70%),radial-gradient(ellipse_60%_70%_at_10%_90%,rgb(103_58_183/0.45),transparent_70%)] px-6 py-30 text-white">
      <div class="relative mx-auto flex max-w-310 flex-col gap-14">
        <div class="grid items-end gap-x-16 gap-y-6 lg:grid-cols-2">
          <div class="flex flex-col gap-4.5">
            <span class="font-geist-mono text-xs tracking-[0.08em] text-fuchsia-300 uppercase">
              Peace of mind
            </span>
            <h2 class="m-0 text-[clamp(38px,4.6vw,64px)] leading-none font-bold tracking-[-0.04em] text-balance">
              Helpful, not nosy.
            </h2>
          </div>
          <a
            href="/docs/assistant/access-and-cost"
            class="self-start text-base font-semibold text-white transition-colors hover:text-fuchsia-300 lg:self-end lg:justify-self-end"
          >
            Read about access, privacy and cost →
          </a>
        </div>

        <div class="grid border-t border-purple-200/14 sm:grid-cols-2 lg:grid-cols-4">
          <For each={assistantTrust}>
            {(item, i) => (
              <div class="flex flex-col gap-2.5 border-b border-purple-200/10 pt-7 pr-7 pb-8">
                <span class="font-geist-mono text-xs text-fuchsia-300/80">
                  {String(i() + 1).padStart(2, '0')}
                </span>
                <span class="text-[19px] font-semibold tracking-[-0.02em]">{item.title}</span>
                <span class="text-[15px] leading-[1.55] text-pretty text-white/58">
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
    <section class="border-t border-hairline bg-[#f1edf7] px-6 py-27.5">
      <div class="mx-auto flex max-w-215 flex-col items-center gap-6 text-center">
        <h2 class="m-0 text-[clamp(32px,3.8vw,52px)] leading-none font-bold tracking-[-0.04em] text-balance">
          It can lend a hand with…
        </h2>
        <p class="m-0 text-lg leading-[1.6] text-ink-muted">
          Pretty much everything in your workspace, as far as your role allows.
        </p>
        <div class="flex flex-wrap justify-center gap-2.5 pt-2">
          <For each={assistantHelpsWith}>
            {(item) => (
              <span class="rounded-full border border-ink/12 bg-white px-4 py-2.25 text-[15px] font-medium text-ink">
                {item}
              </span>
            )}
          </For>
        </div>
      </div>
    </section>
  );
}

export default function AssistantPage() {
  return (
    <div class="bg-mist text-ink antialiased">
      <Nav />
      <main>
        <AssistantHero />
        <ShowcaseSection />
        <TrustSection />
        <HelpsWithSection />
        <CtaSection />
      </main>
      <Footer />
    </div>
  );
}
