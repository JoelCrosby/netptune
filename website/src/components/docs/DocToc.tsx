import { For, Show, createSignal, onCleanup, onMount } from 'solid-js';
import type { DocHeading } from '~/lib/docs-content';

type DocTocProps = {
  headings: DocHeading[];
};

const activationOffset = 120;

export default function DocToc(props: DocTocProps) {
  const [passedId, setPassedId] = createSignal<string>();
  const activeId = () => passedId() ?? props.headings[0]?.id;

  onMount(() => {
    let frame = 0;

    const update = () => {
      frame = 0;
      const passed = props.headings.filter((heading) => {
        const element = document.getElementById(heading.id);
        return element && element.getBoundingClientRect().top <= activationOffset;
      });
      setPassedId(passed.at(-1)?.id);
    };

    const onScroll = () => {
      if (!frame) frame = requestAnimationFrame(update);
    };

    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    onCleanup(() => {
      window.removeEventListener('scroll', onScroll);
      cancelAnimationFrame(frame);
    });
  });

  return (
    <div class="flex flex-col gap-3.5">
      <Show when={props.headings.length > 0}>
        <p class="m-0 font-geist-mono text-[11px] tracking-[0.08em] text-[#8b83a0] uppercase">
          On this page
        </p>
        <nav class="flex flex-col border-l-[1.5px] border-hairline">
          <For each={props.headings}>
            {(heading) => (
              <a
                href={`#${heading.id}`}
                class="-ml-[1.5px] border-l-[1.5px] py-1.5 pl-3.5 text-[13.5px] leading-[1.4] transition-colors hover:text-ink"
                classList={{
                  'border-brand text-ink': activeId() === heading.id,
                  'border-transparent text-ink-subtle': activeId() !== heading.id,
                }}
              >
                {heading.label}
              </a>
            )}
          </For>
        </nav>
      </Show>

      <div class="mt-4.5 flex flex-col gap-2.5 rounded-2xl bg-ink p-4.5 text-white">
        <p class="m-0 text-sm font-semibold">Try it in Netptune</p>
        <p class="m-0 text-[13px] leading-normal text-white/60">Set up a workspace in a minute.</p>
        <a
          href="https://app.netptune.co.uk/auth/register"
          class="self-start rounded-full bg-white px-3.5 py-1.75 text-[13px] font-semibold text-ink transition-colors hover:bg-purple-100"
        >
          Start free →
        </a>
      </div>
    </div>
  );
}
