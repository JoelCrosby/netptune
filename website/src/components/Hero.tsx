import { ArrowRight } from 'lucide-solid';
import Badge from './Badge';
import Button from './Button';

export default function Hero() {
  return (
    <section class="relative overflow-hidden bg-white px-6 pt-20 pb-28 dark:bg-black">
      <div class="absolute inset-0 bg-[radial-gradient(ellipse_80%_50%_at_50%_-5%,rgba(103,58,183,0.08),transparent)] dark:bg-[radial-gradient(ellipse_80%_50%_at_50%_-5%,rgba(103,58,183,0.2),transparent)]" />

      <div class="relative mx-auto max-w-4xl text-center">
        <Badge variant="green" class="mb-7">
          <span class="inline-block h-1.5 w-1.5 rounded-full bg-brand" />
          Open source &middot; MIT licensed
        </Badge>

        <h1 class="mb-6 text-5xl leading-[1.08] font-bold tracking-tight text-slate-900 sm:text-6xl lg:text-7xl dark:text-white">
          Project management
          <br />
          <span class="text-brand">your team will actually use.</span>
        </h1>

        <p class="mx-auto mb-10 max-w-2xl text-xl leading-relaxed text-slate-500 dark:text-white/55">
          Netptune brings your tasks, boards, and team together in one place — with real-time
          collaboration, full audit trails, and the freedom to host it yourself.
        </p>

        <div class="flex flex-col items-center justify-center gap-4 sm:flex-row">
          <Button variant="primary" size="lg" href="#">
            Get started for free
            <ArrowRight size={16} />
          </Button>
          <Button variant="outline" size="lg" href="#self-host">
            Self-host on your own server
          </Button>
        </div>

        <p class="mt-12 text-sm text-slate-400 italic dark:text-white/30">
          "Built for teams who need real control over their tools and their data."
        </p>

        <picture>
          <div data-src="screenshot-dark.webp" />
          <div data-src="screenshot-light.webp" />

          <img
            width={896}
            height={513}
            class="mt-6 rounded-lg border border-neutral-200 shadow-2xl shadow-brand/60 inset-ring-brand dark:border-white/15"
            src="screenshot-light.webp"
            alt="Screenshot of the Netptune interface"
          />
        </picture>
      </div>
    </section>
  );
}
