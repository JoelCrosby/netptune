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

const buttonSizeVariants = {
  small: 'h-8 rounded-sm px-3 text-xs font-medium tracking-wide',
  default: 'h-10 rounded-sm font-medium tracking-wide',
  large: 'h-11.5 rounded-lg font-bold tracking-[.2px]',
};

const buttonBlockVariants = {
  true: 'w-full',
  false: '',
};

export type ButtonSize = keyof typeof buttonSizeVariants;
export type IconButtonSize = 'default' | 'small';

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
      block: buttonBlockVariants,
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
      block: false,
    },
  }
);

export const flatButtonVariants = cva(
  'inline-flex min-w-16 cursor-pointer select-none items-center justify-center gap-2 px-4 text-sm transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      size: buttonSizeVariants,
      block: buttonBlockVariants,
      color: {
        primary:
          'bg-primary text-white hover:bg-primary/90 focus-visible:ring-primary dark:text-neutral-900',
        warn: 'bg-warn text-white hover:bg-warn/90 focus-visible:ring-warn dark:text-neutral-900',
        neutral:
          'bg-foreground/10 text-foreground hover:bg-foreground/15 focus-visible:ring-foreground',
        ghost:
          'bg-transparent text-foreground hover:bg-foreground/10 active:bg-foreground/20 focus-visible:ring-foreground',
        contrast:
          'text-background bg-foreground hover:bg-foreground/80 focus-visible:ring-foreground',
      },
    },
    defaultVariants: {
      color: 'primary',
      size: 'default',
      block: false,
    },
  }
);

export const strokedButtonVariants = cva(
  'border-border inline-flex min-w-16 cursor-pointer select-none items-center justify-center gap-2 border bg-transparent px-4 text-sm transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      size: buttonSizeVariants,
      block: buttonBlockVariants,
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
      size: 'default',
      block: false,
    },
  }
);

export const iconButtonVariants = cva(
  'inline-flex cursor-pointer select-none items-center justify-center transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      size: {
        default: 'h-10 w-10 rounded-full',
        small: 'h-8 w-8 rounded-lg',
      },
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
      size: 'default',
    },
  }
);

// `plain` fades on hover, `lift` goes the other way and brightens to full
// strength, `underline` rules the text, and `soft` picks up a hover background
// and the padding that needs.
export type InlineButtonAppearance = 'plain' | 'lift' | 'underline' | 'soft';

export const inlineButtonVariants = cva(
  'inline-flex cursor-pointer select-none items-center gap-1.5 bg-transparent text-xs transition-colors focus-visible:rounded-xs focus-visible:ring-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      color: {
        primary: 'text-primary focus-visible:ring-primary',
        warn: 'text-warn focus-visible:ring-warn',
        neutral: 'text-foreground focus-visible:ring-foreground',
        contrast: 'text-foreground focus-visible:ring-foreground',
        muted: 'text-muted focus-visible:ring-foreground',
      },
      appearance: {
        plain: 'p-0 hover:opacity-75',
        lift: 'p-0 hover:text-foreground',
        underline: 'p-0 hover:underline',
        soft: 'hover:bg-hover rounded px-2 py-1',
      },
    },
    defaultVariants: {
      color: 'primary',
      appearance: 'plain',
    },
  }
);

export const toolbarButtonVariants = cva(
  'inline-flex h-9 cursor-pointer select-none items-center gap-2 rounded-lg px-3 text-sm font-medium whitespace-nowrap transition-colors duration-140 ease-in-out focus-visible:ring-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      color: {
        primary: 'text-primary hover:bg-primary/10 focus-visible:ring-primary',
        warn: 'text-warn hover:bg-warn/10 focus-visible:ring-warn',
        neutral:
          'text-foreground/80 hover:bg-foreground/10 hover:text-foreground focus-visible:ring-foreground',
        contrast:
          'text-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
      },
    },
    defaultVariants: {
      color: 'neutral',
    },
  }
);
