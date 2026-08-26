import { Component, computed, input, model } from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn } from '../button/button.variants';

export interface SegmentedOption<T extends string = string> {
  value: T;
  label: string;
  count?: number;
}

export type SegmentedVariant = 'pill' | 'outlined' | 'chips';

const groupVariants = cva('', {
  variants: {
    variant: {
      pill: 'bg-hover inline-flex rounded-full p-0.5 text-xs',
      outlined:
        'border-border bg-card flex items-center gap-0.5 rounded-[10px] border p-1 text-sm',
      chips: 'flex items-center gap-1.5 text-sm',
    },
  },
  defaultVariants: {
    variant: 'pill',
  },
});

const optionVariants = cva(
  'focus-visible:ring-primary cursor-pointer transition-colors outline-none focus-visible:ring-2',
  {
    variants: {
      variant: {
        pill: 'rounded-full px-3 py-1.5',
        outlined: 'rounded-lg px-3 py-1.5',
        chips:
          'border-border hover:bg-hover inline-flex h-9 items-center rounded-full border px-3.5',
      },
      selected: {
        true: '',
        false: '',
      },
    },
    compoundVariants: [
      {
        variant: 'pill',
        selected: true,
        class: 'bg-card text-foreground shadow-sm',
      },
      {
        variant: 'pill',
        selected: false,
        class: 'text-muted hover:text-foreground',
      },
      {
        variant: 'outlined',
        selected: true,
        class:
          'border-primary/40 bg-primary/10 text-primary hover:bg-primary/15',
      },
      {
        variant: 'outlined',
        selected: false,
        class:
          'hover:bg-hover hover:text-foreground border-transparent text-muted',
      },
      { variant: 'chips', selected: true, class: 'border-primary bg-hover' },
      { variant: 'chips', selected: false, class: 'text-muted' },
    ],
    defaultVariants: {
      variant: 'pill',
      selected: false,
    },
  }
);

@Component({
  selector: 'app-segmented-control',
  host: {
    '[class]': 'hostClass()',
    role: 'group',
    '[attr.aria-label]': 'ariaLabel()',
  },
  template: `
    @for (option of options(); track option.value) {
      <button
        type="button"
        [class]="optionClass(option.value === value())"
        [attr.aria-pressed]="option.value === value()"
        (click)="value.set(option.value)">
        {{ option.label }}
        @if (option.count !== undefined) {
          <span class="ml-1.5 opacity-70">{{ option.count }}</span>
        }
      </button>
    }
  `,
})
export class SegmentedControlComponent<T extends string = string> {
  readonly options = input.required<SegmentedOption<T>[]>();
  readonly ariaLabel = input<string | null>(null);
  readonly variant = input<SegmentedVariant>('pill');
  readonly class = input('');

  readonly value = model.required<T>();

  protected readonly hostClass = computed(() => {
    return cn(groupVariants({ variant: this.variant() }), this.class());
  });

  protected optionClass(selected: boolean): string {
    return optionVariants({ variant: this.variant(), selected });
  }
}
