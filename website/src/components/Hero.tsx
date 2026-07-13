import { ArrowRight, GitBranch } from 'lucide-solid';
import Button from './Button';

export default function Hero() {
  return (
    <section class="relative overflow-hidden bg-white px-6 pt-20 pb-28 dark:bg-black">
      <div class="absolute inset-0 bg-[radial-gradient(ellipse_80%_50%_at_50%_-5%,rgba(103,58,183,0.12),transparent)] dark:bg-[radial-gradient(ellipse_80%_50%_at_50%_-5%,rgba(103,58,183,0.24),transparent)]" />

      <div class="relative mx-auto max-w-5xl text-center">
        <h1 class="mb-6 text-5xl leading-[1.08] font-bold tracking-tight text-slate-900 sm:text-6xl lg:text-7xl dark:text-white">
          The project workspace
          <br />
          <span class="text-brand">built for developers.</span>
        </h1>

        <p class="mx-auto mb-10 max-w-2xl text-xl leading-relaxed text-slate-500 dark:text-white/55">
          Plan issues, move work through boards and sprints, automate routine updates, and keep the
          whole team synced in real time—from one focused workspace.
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

        <img
          width={896}
          height={513}
          class="mt-14 rounded-lg border border-neutral-200 shadow-2xl shadow-brand/60 inset-ring-brand dark:hidden"
          src="/screenshot-light.webp"
          alt="Screenshot of the Netptune interface"
        />
        <img
          width={896}
          height={513}
          class="mt-14 hidden rounded-lg border border-white/15 shadow-2xl shadow-brand/60 inset-ring-brand dark:block"
          src="/screenshot-dark.webp"
          alt="Screenshot of the Netptune interface"
        />
      </div>
    </section>
  );
}
