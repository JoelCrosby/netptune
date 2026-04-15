import type { JSX } from 'solid-js';

type Variant = 'green' | 'slate' | 'dark';

type BadgeProps = {
  children: JSX.Element;
  variant?: Variant;
  class?: string;
};

const variantClasses: Record<Variant, string> = {
  green:
    'bg-violet-50 text-violet-700 border border-violet-200 dark:bg-violet-500/15 dark:text-violet-300 dark:border-violet-500/25',
  slate:
    'bg-slate-100 text-slate-600 border border-slate-200 dark:bg-white/10 dark:text-white/60 dark:border-white/15',
  dark: 'bg-white/10 text-white border border-white/20',
};

export default function Badge(props: BadgeProps) {
  const variant = () => props.variant ?? 'green';
  return (
    <span
      class={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium ${variantClasses[variant()]} ${props.class ?? ''}`}
    >
      {props.children}
    </span>
  );
}
