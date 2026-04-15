import type { JSX } from 'solid-js';

type DocTableProps = {
  children: JSX.Element;
};

export default function DocTable(props: DocTableProps) {
  return (
    <div class="my-6 overflow-x-auto rounded-lg border border-slate-200 dark:border-white/10">
      <table class="w-full text-sm">{props.children}</table>
    </div>
  );
}
