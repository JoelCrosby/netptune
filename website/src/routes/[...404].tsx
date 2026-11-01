import { HttpStatusCode } from '@solidjs/start';
import Nav from '~/components/Nav';
import Footer from '~/components/Footer';

export default function NotFound() {
  return (
    <div class="bg-mist text-ink antialiased">
      <HttpStatusCode code={404} />
      <Nav />
      <main>
        <section class="home-hero relative overflow-hidden px-6 text-white">
          <div class="home-hero-stars" />
          <div class="home-hero-grid" />

          <div class="relative mx-auto flex max-w-190 flex-col items-center gap-6 py-36 text-center">
            <span class="font-geist-mono text-sm tracking-[0.08em] text-fuchsia-300">404</span>
            <h1 class="m-0 text-[clamp(44px,6vw,80px)] leading-[0.98] font-bold tracking-[-0.045em] text-balance">
              Lost in orbit.
            </h1>
            <p class="m-0 max-w-120 text-[19px] leading-[1.55] text-pretty text-white/66">
              The page you're looking for doesn't exist or has been moved.
            </p>
            <a
              href="/"
              class="mt-2 rounded-full border border-purple-200/28 px-6 py-3.75 text-base font-medium text-white transition-colors hover:bg-purple-200/8"
            >
              ← Back to home
            </a>
          </div>
        </section>
      </main>
      <Footer />
    </div>
  );
}
