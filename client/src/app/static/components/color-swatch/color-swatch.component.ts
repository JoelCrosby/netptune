import { Component, computed, input } from '@angular/core';
import { colorSwatchClass } from '@core/util/colors/colors';

export type ColorSwatchVariant = 'dot' | 'swatch';
export type ColorSwatchSize = 'sm' | 'md';

const variantClasses: Record<ColorSwatchVariant, string> = {
  dot: 'block shrink-0 rounded-full',
  swatch: 'border-border block h-6 w-6 shrink-0 rounded-sm border',
};

// Only the dot is sized; the swatch is a fixed preview tile.
const dotSizeClasses: Record<ColorSwatchSize, string> = {
  sm: 'h-2 w-2',
  md: 'h-2.5 w-2.5',
};

@Component({
  selector: 'app-color-swatch',
  template: '',
  host: {
    'aria-hidden': 'true',
    '[class]': 'className()',
  },
})
export class ColorSwatchComponent {
  readonly color = input.required<string | null | undefined>();
  readonly variant = input<ColorSwatchVariant>('dot');
  readonly size = input<ColorSwatchSize>('md');

  protected readonly className = computed(() => {
    const swatch = colorSwatchClass(this.color());

    if (this.variant() === 'swatch') {
      return `${variantClasses.swatch} ${swatch}`;
    }

    return `${variantClasses.dot} ${dotSizeClasses[this.size()]} ${swatch}`;
  });
}
