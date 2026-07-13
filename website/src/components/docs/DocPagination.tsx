import { Show } from 'solid-js';
import { ChevronLeft, ChevronRight } from 'lucide-solid';

type NavLink = { href: string; label: string };

type DocPaginationProps = {
  prev?: NavLink;
  next?: NavLink;
};

export default function DocPagination(props: DocPaginationProps) {
  return (
    <div class="mt-16 grid grid-cols-2 gap-4 border-t border-slate-200 pt-8 dark:border-white/10">
      <Show when={props.prev} fallback={<span />}>
        <a
          href={props.prev?.href}
          class="group flex items-center gap-2 rounded-xl border border-slate-200 bg-gradient-to-b from-white to-slate-50 px-4 py-3 text-sm text-slate-500 transition-all hover:border-violet-300 hover:text-brand hover:shadow-md hover:shadow-brand/5 dark:border-white/10 dark:from-white/[0.07] dark:to-white/[0.025] dark:text-white/50 dark:hover:border-violet-500/40 dark:hover:text-violet-300"
        >
          <ChevronLeft size={15} class="transition-transform group-hover:-translate-x-0.5" />
          {props.prev?.label}
        </a>
      </Show>
      <Show when={props.next} fallback={<span />}>
        <a
          href={props.next?.href}
          class="group flex items-center justify-end gap-2 rounded-xl border border-slate-200 bg-gradient-to-b from-white to-slate-50 px-4 py-3 text-sm text-slate-500 transition-all hover:border-violet-300 hover:text-brand hover:shadow-md hover:shadow-brand/5 dark:border-white/10 dark:from-white/[0.07] dark:to-white/[0.025] dark:text-white/50 dark:hover:border-violet-500/40 dark:hover:text-violet-300"
        >
          {props.next?.label}
          <ChevronRight size={15} class="transition-transform group-hover:translate-x-0.5" />
        </a>
      </Show>
    </div>
  );
}
