import { Component, HostBinding, input } from '@angular/core';
import { cn, inlineButtonVariants, type ButtonColor } from './button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-inline-button]',
  template: '<ng-content />',
  host: { type: 'button' },
})
export class InlineButtonComponent {
  readonly color = input<ButtonColor>('primary');
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(inlineButtonVariants({ color: this.color() }), this.class());
  }
}
