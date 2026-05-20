import { cva, cx, type CxOptions } from 'class-variance-authority';
import { twMerge } from 'tailwind-merge';

export type ButtonVariant = 'text' | 'filled' | 'outlined';
export type ButtonColor = 'primary' | 'warn' | 'neutral' | 'contrast';
export type ButtonColorInput = ButtonColor | '';
export type FlatButtonColor = ButtonColor | 'ghost';
export type IconButtonColor = ButtonColor | 'default';

export function cn(...inputs: CxOptions): string {
  return twMerge(cx(...inputs));
}

export function coerceButtonColor(
  color: ButtonColorInput | null | undefined
): ButtonColor {
  return color || 'neutral';
}

export function coerceIconButtonColor(color: IconButtonColor): ButtonColor {
  return color === 'default' ? 'neutral' : color;
}

export const buttonHostVariants = cva(
  'inline-flex items-center justify-center transition-colors',
  {
    variants: {
      iconOnly: {
        true: 'h-10 w-10 rounded-full',
        false: 'h-9 rounded px-4 text-sm font-medium',
      },
      variant: {
        text: '',
        filled: '',
        outlined: '',
      },
      color: {
        primary: '',
        warn: '',
        neutral: '',
        contrast: '',
      },
    },
    compoundVariants: [
      {
        iconOnly: true,
        color: 'primary',
        class: 'text-primary hover:bg-primary/8',
      },
      {
        iconOnly: true,
        color: 'warn',
        class: 'text-warn hover:bg-warn/8',
      },
      {
        iconOnly: true,
        color: 'neutral',
        class: 'text-foreground hover:bg-foreground/8',
      },
      {
        iconOnly: false,
        variant: 'filled',
        color: 'primary',
        class: 'bg-primary text-white hover:bg-primary/90',
      },
      {
        iconOnly: false,
        variant: 'filled',
        color: 'warn',
        class: 'bg-warn text-white hover:bg-warn/90',
      },
      {
        iconOnly: false,
        variant: 'filled',
        color: 'neutral',
        class: 'bg-foreground/10 text-foreground hover:bg-foreground/15',
      },
      {
        iconOnly: false,
        variant: 'outlined',
        color: 'primary',
        class: 'border border-primary text-primary hover:bg-primary/8',
      },
      {
        iconOnly: false,
        variant: 'outlined',
        color: 'warn',
        class: 'border border-warn text-warn hover:bg-warn/8',
      },
      {
        iconOnly: false,
        variant: 'outlined',
        color: 'neutral',
        class: 'border border-border text-foreground hover:bg-foreground/8',
      },
      {
        iconOnly: false,
        variant: 'text',
        color: 'primary',
        class: 'text-primary hover:bg-primary/8',
      },
      {
        iconOnly: false,
        variant: 'text',
        color: 'warn',
        class: 'text-warn hover:bg-warn/8',
      },
      {
        iconOnly: false,
        variant: 'text',
        color: 'neutral',
        class: 'text-foreground hover:bg-foreground/8',
      },
    ],
    defaultVariants: {
      iconOnly: false,
      variant: 'text',
      color: 'primary',
    },
  }
);

export const buttonLinkVariants = cva(
  'inline-flex h-10 min-h-9 cursor-pointer select-none items-center justify-center gap-2 rounded-sm px-4 text-sm font-medium transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        text: 'bg-transparent',
        filled: '',
        outlined: 'border bg-transparent',
      },
      color: {
        primary: 'focus-visible:ring-primary',
        warn: 'focus-visible:ring-warn',
        neutral: 'focus-visible:ring-foreground',
        contrast:
          'bg-foreground hover:bg-foreground/90 text-background focus-visible:ring-foreground',
      },
    },
    compoundVariants: [
      {
        variant: 'filled',
        color: 'primary',
        class: 'bg-primary text-white hover:bg-primary/90',
      },
      {
        variant: 'filled',
        color: 'warn',
        class: 'bg-warn text-white hover:bg-warn/90',
      },
      {
        variant: 'filled',
        color: 'neutral',
        class: 'bg-foreground/10 text-foreground hover:bg-foreground/15',
      },
      {
        variant: 'outlined',
        color: 'primary',
        class: 'border-primary text-primary hover:bg-primary/8',
      },
      {
        variant: 'outlined',
        color: 'warn',
        class: 'border-warn text-warn hover:bg-warn/8',
      },
      {
        variant: 'outlined',
        color: 'neutral',
        class: 'border-border text-foreground hover:bg-foreground/8',
      },
      {
        variant: 'text',
        color: 'primary',
        class: 'text-primary hover:bg-primary/8',
      },
      {
        variant: 'text',
        color: 'warn',
        class: 'text-warn hover:bg-warn/8',
      },
      {
        variant: 'text',
        color: 'neutral',
        class: 'text-foreground hover:bg-foreground/8',
      },
    ],
    defaultVariants: {
      variant: 'text',
      color: 'primary',
    },
  }
);

export const flatButtonVariants = cva(
  'inline-flex h-10 min-w-16 cursor-pointer select-none items-center justify-center gap-2 rounded-sm px-4 text-sm font-medium tracking-wide transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      color: {
        primary:
          'bg-primary text-white hover:bg-primary/90 focus-visible:ring-primary dark:text-neutral-900',
        warn: 'bg-warn text-white hover:bg-warn/90 focus-visible:ring-warn dark:text-neutral-900',
        neutral:
          'bg-foreground/10 text-foreground hover:bg-foreground/15 focus-visible:ring-foreground',
        ghost:
          'bg-transparent text-foreground hover:bg-foreground/10 active:bg-foreground/20 focus-visible:ring-foreground',
        contrast:
          'text-background bg-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
      },
    },
    defaultVariants: {
      color: 'primary',
    },
  }
);

export const strokedButtonVariants = cva(
  'border-border inline-flex h-10 min-w-16 cursor-pointer select-none items-center justify-center gap-2 rounded-sm border bg-transparent px-4 text-sm font-medium tracking-wide transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      color: {
        primary: 'text-primary hover:bg-primary/10 focus-visible:ring-primary',
        warn: 'text-warn hover:bg-warn/10 focus-visible:ring-warn',
        neutral:
          'text-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
        contrast:
          'text-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
      },
    },
    defaultVariants: {
      color: 'primary',
    },
  }
);

export const iconButtonVariants = cva(
  'inline-flex h-10 w-10 cursor-pointer select-none items-center justify-center rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      color: {
        primary: 'text-primary hover:bg-primary/10 focus-visible:ring-primary',
        warn: 'text-warn hover:bg-warn/10 focus-visible:ring-warn',
        neutral:
          'text-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
        contrast:
          'text-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
      },
    },
    defaultVariants: {
      color: 'neutral',
    },
  }
);
