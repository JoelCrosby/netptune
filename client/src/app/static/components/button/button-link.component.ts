import { Component, computed, input } from '@angular/core';
import {
  buttonLinkVariants,
  cn,
  coerceButtonColor,
  type ButtonColorInput,
  type ButtonVariant,
} from './button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'a[app-button-link], button[app-button-link]',
  template: '<ng-content />',
  host: { '[class]': 'className()' },
})
export class ButtonLinkComponent {
  readonly variant = input<ButtonVariant>('text');
  readonly color = input<ButtonColorInput>('primary');
  readonly class = input('');

  className = computed(() =>
    cn(
      buttonLinkVariants({
        color: coerceButtonColor(this.color()),
        variant: this.variant(),
      }),
      this.class()
    )
  );
}
