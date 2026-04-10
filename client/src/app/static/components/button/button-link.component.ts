import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { ButtonColor, ButtonVariant } from './button.component';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'a[app-button-link], button[app-button-link]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'className()' },
})
export class ButtonLinkComponent {
  readonly variant = input<ButtonVariant>('text');
  readonly color = input<ButtonColor>('primary');

  className = computed(() => {
    const base =
      'hover:bg-primary/20 min-h-[36px] inline-flex items-center justify-center gap-2 h-10 px-4 text-sm font-medium rounded-sm transition-colors cursor-pointer select-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50';

    const color = this.color();
    const variant = this.variant();

    if (variant === 'filled') {
      return color === 'warn'
        ? `${base} bg-warn text-white hover:bg-warn/90 focus-visible:ring-warn`
        : `${base} bg-primary text-white hover:bg-primary/90 focus-visible:ring-primary`;
    }

    if (variant === 'outlined') {
      return color === 'warn'
        ? `${base} border border-warn text-warn bg-transparent hover:bg-warn/8 focus-visible:ring-warn`
        : `${base} border border-primary text-primary bg-transparent hover:bg-primary/8 focus-visible:ring-primary`;
    }

    if (!color) {
      return `${base} text-foreground bg-transparent hover:bg-foreground/8`;
    }

    return color === 'warn'
      ? `${base} text-warn bg-transparent hover:bg-warn/8 focus-visible:ring-warn`
      : `${base} text-primary bg-transparent hover:bg-primary/8 focus-visible:ring-primary`;
  });
}
