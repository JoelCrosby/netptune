import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';
import {
  cn,
  coerceIconButtonColor,
  iconButtonVariants,
  type IconButtonColor,
} from './button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-icon-button]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconButtonComponent {
  readonly color = input<IconButtonColor>('default');
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(
      iconButtonVariants({ color: coerceIconButtonColor(this.color()) }),
      this.class()
    );
  }
}
