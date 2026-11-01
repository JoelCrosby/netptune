import { For } from 'solid-js';
import { ArrowRight, Sparkles } from 'lucide-solid';
import Nav from '~/components/Nav';
import Footer from '~/components/Footer';
import Button from '~/components/Button';
import Section from '~/components/Section';
import FeatureHighlightsSection from '~/components/FeatureHighlightsSection';
import CtaSection from '~/components/CtaSection';
import {
  assistantHelpsWith,
  assistantPrompts,
  assistantShowcase,
  assistantTrust,
} from '~/data/assistant-page';

function AssistantHero() {
  return (
    <section class="relative overflow-hidden bg-white px-6 pt-24 pb-16 dark:bg-black">
      <div class="hero-stars absolute inset-0" />
      <div class="absolute -top-40 left-1/2 h-96 w-96 -translate-x-1/2 rounded-full bg-brand/25 blur-[120px] dark:bg-brand/35" />
      <div class="absolute top-40 -left-32 h-80 w-80 rounded-full bg-fuchsia-300/20 blur-[120px] dark:bg-fuchsia-700/15" />

      <div class="relative mx-auto max-w-5xl text-center">
        <p class="mb-6 inline-flex items-center gap-2 rounded-full border border-violet-200 bg-violet-50 px-4 py-1.5 text-sm font-medium text-violet-700 dark:border-violet-500/25 dark:bg-violet-500/15 dark:text-violet-300">
          <Sparkles size={14} />
          The Netptune assistant
        </p>
        <h1 class="mb-6 text-4xl leading-[1.1] font-bold tracking-tight text-slate-900 sm:text-5xl lg:text-6xl dark:text-white">
          Knows your work.
          <br />
          <span class="bg-gradient-to-r from-brand via-violet-500 to-fuchsia-500 bg-clip-text text-transparent dark:from-violet-400 dark:via-brand dark:to-fuchsia-400">
            Asks before it touches anything.
          </span>
        </h1>
        <p class="mx-auto mb-10 max-w-2xl text-lg leading-relaxed text-slate-500 sm:text-xl dark:text-white/55">
          Meet the Netptune assistant. Ask it questions, have it draft changes, tidy up the backlog
          or write the boring bits. It does the legwork and you keep the final say.
        </p>

        <div class="flex flex-col items-center justify-center gap-4 sm:flex-row">
          <Button variant="primary" size="lg" href="https://app.netptune.co.uk/auth/register">
            Try it in Netptune
            <ArrowRight size={16} />
          </Button>
          <Button variant="outline" size="lg" href="/docs/assistant">
            Read the guide
          </Button>
        </div>
      </div>
    </section>
  );
}

function PromptStrip() {
  return (
    <section class="bg-white px-6 pb-8 dark:bg-black">
      <div class="mx-auto max-w-4xl text-center">
        <p class="mb-5 text-sm text-slate-400 dark:text-white/40">Things people ask it</p>
        <div class="flex flex-wrap justify-center gap-3">
          <For each={assistantPrompts}>
            {(prompt) => (
              <span class="rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-600 dark:border-white/10 dark:bg-white/5 dark:text-white/65">
                “{prompt}”
              </span>
            )}
          </For>
        </div>
      </div>
    </section>
  );
}

function TrustSection() {
  return (
    <Section class="bg-slate-50 dark:bg-black">
      <div class="mb-12 text-center">
        <p class="mb-3 font-mono text-sm font-semibold tracking-wider text-brand uppercase">
          Peace of mind
        </p>
        <h2 class="text-4xl font-bold tracking-tight text-slate-900 dark:text-white">
          Helpful, not nosy.
        </h2>
      </div>

      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <For each={assistantTrust}>
          {(item) => {
            const Icon = item.icon;
            return (
              <div class="rounded-2xl border border-slate-200/80 bg-white p-6 dark:border-white/10 dark:bg-white/[0.04]">
                <div class="mb-4 flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-violet-100 to-fuchsia-50 text-brand ring-1 ring-brand/10 dark:from-violet-500/20 dark:to-fuchsia-500/10 dark:ring-white/10">
                  <Icon size={18} />
                </div>
                <h3 class="mb-2 font-semibold text-slate-900 dark:text-white">{item.title}</h3>
                <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
                  {item.description}
                </p>
              </div>
            );
          }}
        </For>
      </div>

      <p class="mt-10 text-center text-sm text-slate-500 dark:text-white/50">
        Curious about the details?{' '}
        <a
          href="/docs/assistant/access-and-cost"
          class="font-semibold text-brand underline decoration-violet-300 underline-offset-4 hover:text-brand-dark dark:text-violet-300 dark:decoration-violet-500/50"
        >
          Read about access, privacy and cost
        </a>
      </p>
    </Section>
  );
}

function HelpsWithSection() {
  return (
    <Section class="bg-white dark:bg-black">
      <div class="mx-auto max-w-3xl text-center">
        <h2 class="mb-4 text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
          It can lend a hand with…
        </h2>
        <p class="mb-8 text-slate-500 dark:text-white/55">
          Pretty much everything in your workspace, as far as your role allows.
        </p>
        <div class="flex flex-wrap justify-center gap-3">
          <For each={assistantHelpsWith}>
            {(item) => (
              <span class="rounded-full bg-gradient-to-r from-violet-100 to-fuchsia-50 px-4 py-2 text-sm font-medium text-violet-700 ring-1 ring-brand/10 dark:from-violet-500/20 dark:to-fuchsia-500/10 dark:text-violet-200 dark:ring-white/10">
                {item}
              </span>
            )}
          </For>
        </div>
      </div>
    </Section>
  );
}

export default function AssistantPage() {
  return (
    <div class="bg-white dark:bg-black">
      <Nav />
      <main>
        <AssistantHero />
        <PromptStrip />
        <FeatureHighlightsSection items={assistantShowcase} />
        <TrustSection />
        <HelpsWithSection />
        <CtaSection />
      </main>
      <Footer />
    </div>
  );
}
