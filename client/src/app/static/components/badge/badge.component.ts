import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import {
  cn,
  coerceButtonColor,
  type ButtonColorInput,
} from '../button/button.variants';

export type { ButtonColor as BadgeColor } from '../button/button.variants';

const badgeVariants = cva(
  'inline-flex items-center justify-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium leading-none whitespace-nowrap select-none',
  {
    variants: {
      color: {
        primary: 'bg-primary/10 text-primary',
        warn: 'bg-warn/10 text-warn',
        neutral: 'bg-foreground/10 text-foreground',
        contrast: 'bg-foreground text-background',
      },
    },
    defaultVariants: {
      color: 'neutral',
    },
  }
);

@Component({
  selector: '[appBadge]',
  template: '<ng-content />',
  host: {
    '[class]': 'hostClass()',
  },
})
export class BadgeComponent {
  readonly color = input<ButtonColorInput>('');
  readonly class = input('');

  readonly hostClass = computed(() =>
    cn(badgeVariants({ color: coerceButtonColor(this.color()) }), this.class())
  );
}
