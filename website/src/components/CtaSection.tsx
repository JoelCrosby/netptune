import { GitBranch } from 'lucide-solid';
import Button from './Button';

export default function CtaSection() {
  return (
    <section class="relative overflow-hidden bg-dark px-6 py-24 dark:bg-black">
      <div class="absolute inset-0 bg-[radial-gradient(ellipse_60%_80%_at_50%_100%,rgba(103,58,183,0.35),transparent)]" />
      <div class="absolute bottom-0 left-1/2 h-48 w-96 -translate-x-1/2 rounded-full bg-fuchsia-600/10 blur-[100px]" />
      <div class="relative mx-auto max-w-3xl text-center">
        <h2 class="mb-4 text-4xl leading-tight font-bold text-white sm:text-5xl">
          Run your projects on software
          <br />
          built like your own.
        </h2>
        <p class="mb-10 text-lg text-slate-400 dark:text-white/55">
          Start in the hosted application. The source and deployment path stay open when you need
          them.
        </p>

        <div class="flex flex-col items-center justify-center gap-4 sm:flex-row">
          <Button variant="primary" size="lg" href="https://app.netptune.co.uk/auth/register">
            Create an account
          </Button>
          <Button
            variant="outline"
            size="lg"
            href="https://github.com/JoelCrosby/netptune"
            class="border-slate-600 bg-transparent text-slate-300 hover:border-brand hover:text-neutral-600 dark:border-white/20 dark:text-white/70 dark:hover:border-brand dark:hover:text-white"
          >
            <GitBranch size={16} />
            View source
          </Button>
          <Button
            variant="outline"
            size="lg"
            href="https://app.netptune.co.uk/auth/login"
            class="border-slate-600 bg-transparent text-slate-300 hover:border-brand hover:text-neutral-600 dark:border-white/20 dark:text-white/70 dark:hover:border-brand dark:hover:text-white"
          >
            Sign in
          </Button>
        </div>
      </div>
    </section>
  );
}
