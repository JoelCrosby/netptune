import type { JSX } from 'solid-js';
import { Show } from 'solid-js';
import { useLocation } from '@solidjs/router';
import Footer from '~/components/Footer';
import { findDocNeighbours, findDocSection } from '~/data/docs-nav';
import type { DocHeading } from '~/lib/docs-content';
import DocPagination from './DocPagination';
import DocSidebar from './DocSidebar';
import DocToc from './DocToc';
import DocsNav from './DocsNav';

type DocLayoutProps = {
  title: string;
  description?: string;
  readingTime: string;
  headings: DocHeading[];
  children: JSX.Element;
};

export default function DocLayout(props: DocLayoutProps) {
  const location = useLocation();
  const section = () => findDocSection(location.pathname);
  const neighbours = () => findDocNeighbours(location.pathname);

  return (
    <div class="min-h-screen bg-mist text-ink antialiased">
      <DocsNav />

      <section class="docs-hero relative overflow-hidden px-6 text-white lg:h-104">
        <div class="docs-hero-stars" />
        <div class="docs-hero-planet hidden lg:block" />
        <div class="docs-hero-ring hidden lg:block" />
        <div class="relative mx-auto flex max-w-330 flex-col gap-4.5 pt-16 pb-15">
          <p class="m-0 flex items-center gap-2 font-geist-mono text-xs tracking-[0.06em] text-purple-200/60 uppercase">
            <a href="/docs" class="whitespace-nowrap text-inherit hover:text-purple-100">
              Docs
            </a>
            <Show when={section()}>
              <span class="text-fuchsia-500">/</span>
              <span class="whitespace-nowrap text-fuchsia-300">{section()}</span>
            </Show>
          </p>
          <h1 class="m-0 max-w-190 text-[clamp(40px,5.2vw,72px)] leading-[0.98] font-bold tracking-[-0.045em] text-balance">
            {props.title}
          </h1>
          <Show when={props.description}>
            <p class="m-0 max-w-155 text-[19px] leading-[1.55] text-pretty text-white/66">
              {props.description}
            </p>
          </Show>
          <p class="m-0 pt-1.5 font-geist-mono text-xs text-white/42">{props.readingTime}</p>
        </div>
      </section>

      <div class="mx-auto flex max-w-330 items-start gap-12 px-6 pt-12 pb-30">
        <aside class="sticky top-24 hidden max-h-[calc(100vh-7rem)] w-55 shrink-0 overflow-y-auto lg:block">
          <DocSidebar />
        </aside>

        <main class="max-w-190 min-w-0 flex-1">
          <details class="group mb-10 rounded-2xl border border-[#e9e3f2] bg-white lg:hidden">
            <summary class="flex cursor-pointer list-none items-center justify-between px-5 py-3.5 text-sm font-medium text-ink [&::-webkit-details-marker]:hidden">
              Browse the docs
              <span class="font-geist-mono text-xs text-ink-subtle group-open:hidden">Show</span>
              <span class="hidden font-geist-mono text-xs text-ink-subtle group-open:inline">
                Hide
              </span>
            </summary>
            <div class="border-t border-hairline px-2 py-5">
              <DocSidebar />
            </div>
          </details>

          {props.children}

          <DocPagination prev={neighbours().prev} next={neighbours().next} />
        </main>

        <aside class="sticky top-24 hidden w-52 shrink-0 xl:block">
          <DocToc headings={props.headings} />
        </aside>
      </div>

      <Footer />
    </div>
  );
}
