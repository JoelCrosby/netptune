import { Component, computed, input } from '@angular/core';

export type ColorSwatchVariant = 'dot' | 'swatch';

const variantClasses: Record<ColorSwatchVariant, string> = {
  dot: 'block h-2.5 w-2.5 shrink-0 rounded-full',
  swatch: 'border-border block h-6 w-6 shrink-0 rounded-sm border',
};

@Component({
  selector: 'app-color-swatch',
  template: '',
  host: {
    'aria-hidden': 'true',
    '[class]': 'className()',
    '[style.background-color]': "color() ?? '#64748b'",
  },
})
export class ColorSwatchComponent {
  readonly color = input.required<string | null | undefined>();
  readonly variant = input<ColorSwatchVariant>('dot');

  protected readonly className = computed(() => variantClasses[this.variant()]);
}
