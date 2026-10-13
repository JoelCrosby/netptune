import {
  booleanAttribute,
  Component,
  computed,
  input,
  model,
} from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn } from '../button/button.variants';
import { ColorSwatchComponent } from '../color-swatch/color-swatch.component';

export interface SegmentedOption<T extends string = string> {
  value: T;
  label: string;
  count?: number;
  /** Renders a colour swatch before the label. */
  color?: string | null;
}

export type SegmentedVariant = 'outlined' | 'chips' | 'subtle';

const groupVariants = cva('', {
  variants: {
    variant: {
      outlined:
        'border-border bg-card flex items-center gap-0.5 rounded-[10px] border p-1 text-sm',
      chips: 'flex items-center gap-1.5 text-sm',
      subtle:
        'border-border inline-flex h-[30px] items-center gap-0.5 rounded-lg border p-0.5 text-[12.5px]',
    },
    wrap: {
      true: 'flex-wrap',
      false: '',
    },
  },
  defaultVariants: {
    variant: 'outlined',
    wrap: false,
  },
});

const optionVariants = cva(
  'focus-visible:ring-primary inline-flex cursor-pointer items-center gap-2 whitespace-nowrap transition-colors outline-none focus-visible:ring-2',
  {
    variants: {
      variant: {
        outlined: 'rounded-lg px-3 py-1.5',
        chips:
          'border-border hover:bg-hover inline-flex h-9 items-center rounded-full border px-3.5',
        subtle: 'h-6 rounded-md px-2.5 font-medium',
      },
      selected: {
        true: '',
        false: '',
      },
    },
    compoundVariants: [
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
      {
        variant: 'subtle',
        selected: true,
        class: 'bg-foreground/8 text-foreground',
      },
      {
        variant: 'subtle',
        selected: false,
        class: 'text-foreground/60 hover:text-foreground',
      },
    ],
    defaultVariants: {
      variant: 'outlined',
      selected: false,
    },
  }
);

@Component({
  selector: 'app-segmented-control',
  imports: [ColorSwatchComponent],
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
        @if (option.color !== undefined) {
          <app-color-swatch size="sm" [color]="option.color" />
        }
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
  readonly variant = input<SegmentedVariant>('outlined');
  readonly wrap = input(false, { transform: booleanAttribute });
  readonly class = input('');

  readonly value = model.required<T>();

  protected readonly hostClass = computed(() => {
    return cn(
      groupVariants({ variant: this.variant(), wrap: this.wrap() }),
      this.class()
    );
  });

  protected optionClass(selected: boolean): string {
    return optionVariants({ variant: this.variant(), selected });
  }
}
