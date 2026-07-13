import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn, type ButtonColor } from '../button/button.variants';

export type BadgeColor = ButtonColor | 'success' | 'info' | 'pending';
export type BadgeShape = 'pill' | 'rounded';

const badgeVariants = cva(
  'inline-flex items-center justify-center gap-1 px-2 py-0.5 text-xs font-medium leading-none whitespace-nowrap select-none',
  {
    variants: {
      color: {
        primary: 'bg-primary/10 text-primary',
        warn: 'bg-warn/10 text-warn',
        neutral: 'bg-foreground/10 text-foreground',
        contrast: 'bg-foreground text-background',
        success: 'bg-green-500/10 text-green-600 dark:text-green-400',
        info: 'bg-blue-500/10 text-blue-600 dark:text-blue-400',
        pending: 'bg-yellow-500/15 text-yellow-700 dark:text-yellow-300',
      },
      shape: {
        pill: 'rounded-full',
        rounded: 'rounded',
      },
    },
    defaultVariants: {
      color: 'neutral',
      shape: 'pill',
    },
  }
);

@Component({
  selector: 'app-badge',
  template: '<ng-content />',
  host: {
    '[class]': 'hostClass()',
  },
})
export class BadgeComponent {
  readonly color = input<BadgeColor | ''>('');
  readonly shape = input<BadgeShape>('pill');
  readonly class = input('');

  readonly hostClass = computed(() =>
    cn(
      badgeVariants({
        color: this.color() || 'neutral',
        shape: this.shape(),
      }),
      this.class()
    )
  );
}
