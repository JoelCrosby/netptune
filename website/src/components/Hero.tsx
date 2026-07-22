import { ArrowRight, GitBranch } from 'lucide-solid';
import Button from './Button';

export default function Hero() {
  return (
    <section class="relative overflow-hidden bg-white px-6 pt-24 pb-28 dark:bg-black">
      <div class="hero-grid absolute inset-0" />
      <div class="absolute -top-40 left-1/2 h-96 w-96 -translate-x-1/2 rounded-full bg-brand/25 blur-[120px] dark:bg-brand/35" />
      <div class="absolute top-48 -right-32 h-80 w-80 rounded-full bg-fuchsia-300/20 blur-[120px] dark:bg-fuchsia-700/15" />

      <div class="relative mx-auto max-w-5xl text-center">
        <h1 class="mb-6 text-5xl leading-[1.08] font-bold tracking-tight text-slate-900 sm:text-6xl lg:text-7xl dark:text-white">
          The project workspace
          <br />
          <span class="bg-gradient-to-r from-brand via-violet-500 to-fuchsia-500 bg-clip-text text-transparent dark:from-violet-400 dark:via-brand dark:to-fuchsia-400">
            built for developers.
          </span>
        </h1>

        <p class="mx-auto mb-10 max-w-2xl text-xl leading-relaxed text-slate-500 dark:text-white/55">
          Plan work across boards, sprints, calendars, and roadmaps. Automate the handoffs, measure
          delivery, and connect your tools through a scoped API—from one focused workspace.
        </p>

        <div class="flex flex-col items-center justify-center gap-4 sm:flex-row">
          <Button variant="primary" size="lg" href="https://app.netptune.co.uk/auth/login">
            Start using Netptune
            <ArrowRight size={16} />
          </Button>
          <Button variant="outline" size="lg" href="https://github.com/JoelCrosby/netptune">
            <GitBranch size={16} />
            View source
          </Button>
        </div>

        <div class="mt-16 rounded-2xl bg-gradient-to-br from-brand/40 via-fuchsia-300/30 to-sky-300/30 p-px shadow-2xl shadow-brand/30 dark:from-violet-400/35 dark:via-fuchsia-500/20 dark:to-sky-500/20">
          <div class="overflow-hidden rounded-[calc(1rem-1px)] bg-white/80 p-2 backdrop-blur sm:p-3 dark:bg-white/5">
            <img
              width={896}
              height={513}
              class="w-full rounded-xl border border-neutral-200 shadow-xl dark:hidden"
              src="/screenshot-light.webp"
              alt="Screenshot of the Netptune interface"
            />
            <img
              width={896}
              height={513}
              class="hidden w-full rounded-xl border border-white/15 shadow-xl dark:block"
              src="/screenshot-dark.webp"
              alt="Screenshot of the Netptune interface"
            />
          </div>
        </div>
      </div>
    </section>
  );
}
