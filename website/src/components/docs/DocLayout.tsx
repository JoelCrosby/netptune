import type { JSX } from 'solid-js';
import { Show } from 'solid-js';
import Footer from '~/components/Footer';
import Nav from '~/components/Nav';
import DocSidebar from './DocSidebar';

type DocLayoutProps = {
  title: string;
  description?: string;
  children: JSX.Element;
};

export default function DocLayout(props: DocLayoutProps) {
  return (
    <div class="relative overflow-hidden bg-white dark:bg-black">
      <Nav />
      <div class="pointer-events-none absolute top-16 left-1/2 h-80 w-[48rem] -translate-x-1/2 rounded-full bg-brand/8 blur-[120px] dark:bg-brand/12" />
      <div class="relative mx-auto max-w-6xl px-6 py-14">
        <div class="flex gap-14">
          <aside class="hidden w-52 shrink-0 lg:block">
            <div class="sticky top-24">
              <DocSidebar />
            </div>
          </aside>
          <main class="min-w-0 flex-1">
            <header class="relative mb-10 overflow-hidden rounded-2xl border border-violet-200/70 bg-gradient-to-br from-violet-50 via-white to-fuchsia-50/50 px-7 py-8 dark:border-white/10 dark:from-violet-500/10 dark:via-white/[0.035] dark:to-fuchsia-500/[0.06]">
              <div class="absolute -top-20 -right-16 h-48 w-48 rounded-full bg-brand/10 blur-3xl dark:bg-brand/20" />
              <h1 class="relative text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
                {props.title}
              </h1>
              <Show when={props.description}>
                <p class="relative mt-3 max-w-3xl text-lg leading-relaxed text-slate-500 dark:text-white/55">
                  {props.description}
                </p>
              </Show>
            </header>
            {props.children}
          </main>
        </div>
      </div>
      <Footer />
    </div>
  );
}
