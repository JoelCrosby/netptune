import { GitBranch } from 'lucide-solid';
import Button from './Button';

export default function CtaSection() {
  return (
    <section class="bg-dark px-6 py-20 dark:bg-black">
      <div class="mx-auto max-w-3xl text-center">
        <h2 class="mb-4 text-4xl leading-tight font-bold text-white sm:text-5xl">
          Start organizing work
          <br />
          that actually ships.
        </h2>
        <p class="mb-10 text-lg text-slate-400 dark:text-white/55">
          Use the hosted application, deploy the Helm chart, or contribute to the project.
        </p>

        <div class="flex flex-col items-center justify-center gap-4 sm:flex-row">
          <Button variant="primary" size="lg" href="https://github.com/JoelCrosby/netptune">
            <GitBranch size={16} />
            View on GitHub
          </Button>
          <Button
            variant="outline"
            size="lg"
            href="/docs"
            class="border-slate-600 bg-transparent text-slate-300 hover:border-brand hover:text-neutral-600 dark:border-white/20 dark:text-white/70 dark:hover:border-brand dark:hover:text-white"
          >
            Read the docs
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
