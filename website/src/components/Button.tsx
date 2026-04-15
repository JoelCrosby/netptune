import type { JSX } from 'solid-js';
import { Show, splitProps } from 'solid-js';

type Variant = 'primary' | 'outline' | 'ghost';
type Size = 'sm' | 'md' | 'lg';

type ButtonProps = {
  variant?: Variant;
  size?: Size;
  href?: string;
  class?: string;
  children: JSX.Element;
} & JSX.ButtonHTMLAttributes<HTMLButtonElement>;

const variantClasses: Record<Variant, string> = {
  primary: 'bg-brand hover:bg-brand-dark text-white hover:bg-brand-dark shadow-sm',
  outline:
    'border border-slate-300 text-slate-800 hover:border-brand hover:text-brand bg-white dark:border-white/20 dark:text-white dark:bg-transparent dark:hover:border-brand dark:hover:text-brand',
  ghost:
    'text-slate-600 hover:text-brand hover:bg-violet-50 dark:text-white/60 dark:hover:text-brand dark:hover:bg-violet-500/10',
};

const sizeClasses: Record<Size, string> = {
  sm: 'px-4 py-2 text-sm',
  md: 'px-5 py-2.5 text-sm',
  lg: 'px-6 py-3 text-base',
};

export default function Button(props: ButtonProps) {
  const [local, rest] = splitProps(props, ['variant', 'size', 'href', 'class', 'children']);

  const variant = () => local.variant ?? 'primary';
  const size = () => local.size ?? 'md';

  const classes = () =>
    `inline-flex items-center justify-center gap-2 rounded font-medium transition-colors cursor-pointer ${variantClasses[variant()]} ${sizeClasses[size()]} ${local.class ?? ''}`;

  return (
    <Show
      when={local.href}
      fallback={
        <button class={classes()} {...rest}>
          {local.children}
        </button>
      }
    >
      <a href={local.href} class={classes()}>
        {local.children}
      </a>
    </Show>
  );
}
