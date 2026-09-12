import { booleanAttribute, Component, HostBinding, input } from '@angular/core';
import {
  cn,
  flatButtonVariants,
  type ButtonSize,
  type FlatButtonColor,
} from './button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-flat-button], a[app-flat-button]',
  template: '<ng-content />',
})
export class FlatButtonComponent {
  readonly color = input<FlatButtonColor>('primary');
  readonly size = input<ButtonSize>('default');
  readonly block = input(false, { transform: booleanAttribute });
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(
      flatButtonVariants({
        color: this.color(),
        size: this.size(),
        block: this.block(),
      }),
      this.class()
    );
  }
}
