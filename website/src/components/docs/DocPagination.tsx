import { Show } from 'solid-js';
import type { DocLink } from '~/data/docs-nav';

type DocPaginationProps = {
  prev?: DocLink;
  next?: DocLink;
};

export default function DocPagination(props: DocPaginationProps) {
  return (
    <div class="mt-18 grid gap-3 border-t border-hairline pt-8 sm:grid-cols-2">
      <Show when={props.prev} fallback={<span />}>
        {(prev) => (
          <a
            href={prev().href}
            class="flex flex-col gap-1.5 rounded-[18px] border border-[#e9e3f2] bg-white px-5.5 py-5 text-ink transition-shadow hover:border-violet-300 hover:shadow-[0_14px_40px_-18px_rgb(103_58_183/0.4)]"
          >
            <span class="font-geist-mono text-[11px] tracking-[0.08em] text-[#8b83a0] uppercase">
              ← Previous
            </span>
            <span class="text-lg font-[650] tracking-[-0.02em]">{prev().label}</span>
          </a>
        )}
      </Show>
      <Show when={props.next}>
        {(next) => (
          <a
            href={next().href}
            class="flex flex-col items-end gap-1.5 rounded-[18px] bg-ink px-5.5 py-5 text-white transition-shadow hover:shadow-[0_14px_40px_-18px_rgb(103_58_183/0.6)]"
          >
            <span class="font-geist-mono text-[11px] tracking-[0.08em] text-violet-300 uppercase">
              Next →
            </span>
            <span class="text-lg font-[650] tracking-[-0.02em]">{next().label}</span>
          </a>
        )}
      </Show>
    </div>
  );
}
