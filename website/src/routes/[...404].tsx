import { ArrowLeft } from 'lucide-solid';
import Nav from '~/components/Nav';
import Footer from '~/components/Footer';
import Button from '~/components/Button';

export default function NotFound() {
  return (
    <div class="bg-white dark:bg-black">
      <Nav />
      <main>
        <section class="relative overflow-hidden bg-white px-6 py-32 dark:bg-black">
          <div class="absolute inset-0 bg-[radial-gradient(ellipse_80%_50%_at_50%_-5%,rgba(103,58,183,0.08),transparent)] dark:bg-[radial-gradient(ellipse_80%_50%_at_50%_-5%,rgba(103,58,183,0.2),transparent)]" />

          <div class="relative mx-auto max-w-2xl text-center">
            <p class="mb-4 text-8xl font-bold text-brand">404</p>

            <h1 class="mb-4 text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl dark:text-white">
              Page not found
            </h1>

            <p class="mb-10 text-lg leading-relaxed text-slate-500 dark:text-white/55">
              The page you're looking for doesn't exist or has been moved.
            </p>

            <Button variant="outline" size="lg" href="/">
              <ArrowLeft size={16} />
              Back to home
            </Button>
          </div>
        </section>
      </main>
      <Footer />
    </div>
  );
}
