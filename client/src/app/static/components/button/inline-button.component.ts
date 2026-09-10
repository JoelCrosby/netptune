import { Component, HostBinding, input } from '@angular/core';
import {
  cn,
  inlineButtonVariants,
  type InlineButtonAppearance,
} from './button.variants';

export type InlineButtonColor =
  'primary' | 'warn' | 'neutral' | 'contrast' | 'muted';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-inline-button]',
  template: '<ng-content />',
  host: { type: 'button' },
})
export class InlineButtonComponent {
  readonly color = input<InlineButtonColor>('primary');
  readonly appearance = input<InlineButtonAppearance>('plain');
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(
      inlineButtonVariants({
        color: this.color(),
        appearance: this.appearance(),
      }),
      this.class()
    );
  }
}
