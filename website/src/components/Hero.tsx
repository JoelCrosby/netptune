import { For } from 'solid-js';

import HeroGlobe from './HeroGlobe';

interface OrbitChip {
  label: string;
  dot: string;
  orbit: 'outer' | 'inner';
  duration: number;
  delay: number;
}

const orbitChips: OrbitChip[] = [
  { label: 'NP-142 → Done', dot: '#86efac', orbit: 'outer', duration: 26, delay: 0 },
  { label: 'PR #318 merged · ✓ checks', dot: '#c4b5fd', orbit: 'outer', duration: 26, delay: -8.7 },
  { label: '✦ 3 changes to review', dot: '#f0abfc', orbit: 'outer', duration: 26, delay: -17.4 },
  { label: 'Sprint 14 started', dot: '#7dd3fc', orbit: 'inner', duration: 19, delay: -4 },
  { label: 'Release runbook edited', dot: '#fde68a', orbit: 'inner', duration: 19, delay: -13.5 },
];

function OrbitRings(props: { half: 'back' | 'front' }) {
  return (
    <>
      <div class={`hero-orbit-ring hero-orbit-ring--outer hero-orbit-ring--${props.half}`} />
      <div class={`hero-orbit-ring hero-orbit-ring--inner hero-orbit-ring--${props.half}`} />
    </>
  );
}

function HeroPlanet() {
  return (
    <div class="hero-orbit" aria-hidden="true">
      <div class="hero-orbit-glow" />
      <div class="hero-orbit-tilt">
        <OrbitRings half="back" />
        <div class="hero-planet">
          <div class="hero-planet-shade" />
          <HeroGlobe />
          <div class="hero-planet-rim" />
        </div>
        <OrbitRings half="front" />

        <For each={orbitChips}>
          {(chip, i) => (
            <div
              class={`hero-orbit-chip hero-orbit-chip--${chip.orbit}`}
              style={{
                '--orbit-duration': `${chip.duration}s`,
                '--orbit-delay': `${chip.delay}s`,
                '--orbit-rest': `${60 + i() * 9}%`,
              }}
            >
              <div class="flex rotate-16 items-center gap-2 rounded-full border border-purple-200/20 bg-[rgb(22_14_40/0.82)] px-3.5 py-2 font-geist-mono text-[12.5px] whitespace-nowrap text-white shadow-[0_8px_30px_rgb(0_0_0/0.4)] backdrop-blur-sm">
                <span
                  class="size-1.75 rounded-full"
                  style={{ background: chip.dot, 'box-shadow': `0 0 10px ${chip.dot}` }}
                />
                {chip.label}
              </div>
            </div>
          )}
        </For>
      </div>
    </div>
  );
}

export default function Hero() {
  return (
    <section class="home-hero relative overflow-hidden px-6 text-white">
      <div class="home-hero-stars" />
      <div class="home-hero-grid" />

      <div class="relative mx-auto grid max-w-310 items-center gap-6 pt-18 pb-24 lg:min-h-180 lg:grid-cols-2">
        <div class="relative z-10 flex flex-col items-start gap-7">
          <a
            href="#assistant"
            class="inline-flex items-center gap-2.5 rounded-full border border-purple-200/16 bg-purple-200/7 py-1.25 pr-3.5 pl-1.25 text-[13px] text-white/80 transition-colors hover:bg-purple-200/12 hover:text-white"
          >
            <span class="rounded-full bg-fuchsia-500 px-2 py-0.75 font-geist-mono text-[11px] font-medium text-white">
              NEW
            </span>
            An assistant that asks before it changes a thing
            <span class="text-purple-200">→</span>
          </a>

          <h1 class="m-0 font-geist text-[clamp(48px,6.6vw,92px)] leading-[0.98] font-bold tracking-[-0.045em] text-balance">
            The project workspace{' '}
            <span class="bg-[linear-gradient(100deg,#c4b5fd_0%,#a78bfa_30%,#e879f9_70%,#f0abfc_100%)] bg-clip-text text-transparent">
              built for developers.
            </span>
          </h1>

          <p class="m-0 max-w-130 text-[19px] leading-[1.55] text-pretty text-white/66">
            Boards, sprints, a calendar, a roadmap, and pages for everything you write down. Plus an
            assistant that does the legwork and asks before it changes a thing.
          </p>

          <div class="flex flex-wrap gap-3">
            <a
              href="https://app.netptune.co.uk/auth/login"
              class="inline-flex items-center gap-2.5 rounded-full bg-white px-6 py-3.75 text-base font-semibold text-ink shadow-[0_10px_40px_rgb(167_139_250/0.35)] transition-colors hover:bg-purple-100"
            >
              Start using Netptune <span>→</span>
            </a>
            <a
              href="/assistant"
              class="inline-flex items-center gap-2.5 rounded-full border border-purple-200/28 px-6 py-3.75 text-base font-medium text-white transition-colors hover:bg-purple-200/8"
            >
              Meet the assistant
            </a>
          </div>
        </div>

        <div class="relative h-110 min-w-0 sm:h-160">
          <HeroPlanet />
        </div>
      </div>
    </section>
  );
}
