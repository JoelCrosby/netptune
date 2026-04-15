import type { JSX } from 'solid-js';
import { AlertTriangle, Info, Lightbulb } from 'lucide-solid';

type CalloutType = 'note' | 'warning' | 'tip';

type CalloutProps = {
  type?: CalloutType;
  children: JSX.Element;
};

const calloutConfig = {
  note: {
    wrapperClass: 'bg-blue-50 border-blue-200 dark:bg-blue-500/10 dark:border-blue-500/25',
    iconClass: 'text-blue-500 dark:text-blue-400',
  },
  warning: {
    wrapperClass: 'bg-amber-50 border-amber-200 dark:bg-amber-500/10 dark:border-amber-500/25',
    iconClass: 'text-amber-500 dark:text-amber-400',
  },
  tip: {
    wrapperClass: 'bg-green-50 border-green-200 dark:bg-green-500/10 dark:border-green-500/25',
    iconClass: 'text-green-600 dark:text-green-400',
  },
};

const icons = {
  note: (cls: string) => <Info size={16} class={cls} />,
  warning: (cls: string) => <AlertTriangle size={16} class={cls} />,
  tip: (cls: string) => <Lightbulb size={16} class={cls} />,
};

export default function Callout(props: CalloutProps) {
  const type = () => props.type ?? 'note';
  const config = () => calloutConfig[type()];

  return (
    <div class={`my-6 flex gap-3 rounded-lg border p-4 ${config().wrapperClass}`}>
      <span class={`mt-0.5 shrink-0 ${config().iconClass}`}>
        {icons[type()](config().iconClass)}
      </span>
      <div class="text-sm leading-relaxed text-slate-700 dark:text-white/70">{props.children}</div>
    </div>
  );
}
