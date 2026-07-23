import { Component, computed, input } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from './button/button.variants';

export type IconCircleAppearance = 'soft' | 'solid';
export type IconCircleSize = 'small' | 'medium';

@Component({
  selector: 'app-icon-circle',
  imports: [LucideDynamicIcon],
  template: `
    <span [class]="circleClass()" aria-hidden="true">
      <svg [lucideIcon]="icon()" [class]="iconClass()"></svg>
    </span>
  `,
  styles: ``,
})
export class IconCircleComponent {
  readonly icon = input.required<LucideIconInput>();
  readonly appearance = input<IconCircleAppearance>('soft');
  readonly size = input<IconCircleSize>('medium');
  readonly class = input('');

  readonly circleClass = computed(() =>
    cn(
      'relative flex shrink-0 items-center justify-center rounded-full',
      this.size() === 'small' ? 'h-6 w-6' : 'h-8 w-8',
      this.appearance() === 'solid'
        ? 'bg-primary text-primary-foreground shadow-sm'
        : 'bg-primary/10 text-primary',
      this.class()
    )
  );

  readonly iconClass = computed(() =>
    this.size() === 'small' ? 'h-3.5 w-3.5' : 'h-4 w-4'
  );
}
