import type { JSX } from 'solid-js';
import { Show } from 'solid-js';

type CodeBlockProps = {
  language?: string;
  children: JSX.Element;
};

export default function CodeBlock(props: CodeBlockProps) {
  return (
    <div class="my-6 overflow-hidden rounded-lg border border-slate-200 dark:border-white/10">
      <Show when={props.language}>
        <div class="border-b border-white/10 bg-slate-900 px-4 py-2">
          <span class="text-xs font-medium text-slate-400">{props.language}</span>
        </div>
      </Show>
      <pre class="overflow-x-auto bg-slate-950 p-4 text-sm leading-relaxed text-slate-300">
        <code>{props.children}</code>
      </pre>
    </div>
  );
}
