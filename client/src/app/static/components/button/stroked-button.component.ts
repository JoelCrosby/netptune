import { booleanAttribute, Component, HostBinding, input } from '@angular/core';
import {
  cn,
  strokedButtonVariants,
  type ButtonColor,
  type ButtonSize,
} from './button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-stroked-button], a[app-stroked-button]',
  template: '<ng-content />',
})
export class StrokedButtonComponent {
  readonly color = input<ButtonColor>('primary');
  readonly size = input<ButtonSize>('default');
  readonly block = input(false, { transform: booleanAttribute });
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(
      strokedButtonVariants({
        color: this.color(),
        size: this.size(),
        block: this.block(),
      }),
      this.class()
    );
  }
}
