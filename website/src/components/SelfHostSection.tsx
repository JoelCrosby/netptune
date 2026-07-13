import { GitBranch, Server } from 'lucide-solid';
import Button from './Button';

export default function SelfHostSection() {
  return (
    <section id="architecture" class="bg-dark px-6 py-14 dark:bg-black">
      <div class="mx-auto max-w-6xl">
        <div class="grid items-center gap-10 lg:grid-cols-[1.2fr_1fr]">
          <div>
            <p class="mb-3 font-mono text-sm font-semibold tracking-wider text-brand uppercase">
              Open architecture
            </p>
            <h2 class="mb-4 text-3xl leading-tight font-bold text-white">
              A real application stack, in one repository.
            </h2>
            <p class="max-w-2xl leading-relaxed text-slate-400 dark:text-white/55">
              The Angular client, .NET services, workers, and deployment definitions are public and
              MIT licensed. Use the hosted application by default; a maintained Helm chart is there
              when operating your own instance is the right choice.
            </p>
          </div>

          <div class="rounded-xl border border-slate-700/60 bg-dark-surface/60 p-5 dark:border-white/10 dark:bg-white/5">
            <div class="mb-5 flex items-center gap-3">
              <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-brand/15 text-brand">
                <Server size={18} />
              </div>
              <div>
                <p class="text-sm font-semibold text-white">Developer-owned by design</p>
                <p class="text-xs text-slate-500">Inspect, contribute, or deploy</p>
              </div>
            </div>
            <div class="flex flex-col gap-3 sm:flex-row lg:flex-col xl:flex-row">
              <Button variant="primary" size="md" href="https://github.com/JoelCrosby/netptune">
                <GitBranch size={15} />
                Browse the source
              </Button>
              <Button
                variant="outline"
                size="md"
                href="/docs"
                class="border-slate-600 bg-transparent text-slate-300 hover:border-brand hover:text-white dark:border-white/20 dark:text-white/70"
              >
                Deployment docs
              </Button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
