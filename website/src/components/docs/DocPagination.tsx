import { Show } from 'solid-js';
import { ChevronLeft, ChevronRight } from 'lucide-solid';

type NavLink = { href: string; label: string };

type DocPaginationProps = {
  prev?: NavLink;
  next?: NavLink;
};

export default function DocPagination(props: DocPaginationProps) {
  return (
    <div class="mt-16 flex items-center justify-between border-t border-slate-200 pt-8 dark:border-white/10">
      <Show when={props.prev} fallback={<span />}>
        <a
          href={props.prev?.href}
          class="flex items-center gap-1.5 text-sm text-slate-500 transition-colors hover:text-brand dark:text-white/45 dark:hover:text-white"
        >
          <ChevronLeft size={15} />
          {props.prev?.label}
        </a>
      </Show>
      <Show when={props.next} fallback={<span />}>
        <a
          href={props.next?.href}
          class="flex items-center gap-1.5 text-sm text-slate-500 transition-colors hover:text-brand dark:text-white/45 dark:hover:text-white"
        >
          {props.next?.label}
          <ChevronRight size={15} />
        </a>
      </Show>
    </div>
  );
}
