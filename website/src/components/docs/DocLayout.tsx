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
    <div class="bg-white dark:bg-black">
      <Nav />
      <div class="mx-auto max-w-6xl px-6 py-14">
        <div class="flex gap-14">
          <aside class="hidden w-52 shrink-0 lg:block">
            <div class="sticky top-24">
              <DocSidebar />
            </div>
          </aside>
          <main class="min-w-0 flex-1">
            <header class="mb-10 border-b border-slate-200 pb-8 dark:border-white/10">
              <h1 class="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
                {props.title}
              </h1>
              <Show when={props.description}>
                <p class="mt-3 text-lg text-slate-500 dark:text-white/55">{props.description}</p>
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
