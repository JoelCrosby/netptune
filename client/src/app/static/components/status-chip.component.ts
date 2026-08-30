import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import { ColorSwatchComponent } from './color-swatch/color-swatch.component';
import { cn } from './button/button.variants';

export type StatusChipTone = 'neutral' | 'primary';

const statusChipVariants = cva(
  'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs',
  {
    variants: {
      tone: {
        neutral: 'bg-foreground/6 text-muted',
        primary: 'bg-primary/16 text-primary font-medium',
      },
    },
    defaultVariants: {
      tone: 'neutral',
    },
  }
);

@Component({
  selector: 'app-status-chip',
  imports: [ColorSwatchComponent],
  host: {
    '[class]': 'hostClass()',
  },
  template: `
    <app-color-swatch size="sm" [color]="color()" />
    {{ name() }}
  `,
})
export class StatusChipComponent {
  readonly name = input.required<string>();
  readonly color = input<string | null>();
  readonly tone = input<StatusChipTone>('neutral');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(statusChipVariants({ tone: this.tone() }), this.class());
  });
}
