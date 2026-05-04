import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';
import {
  cn,
  flatButtonVariants,
  type FlatButtonColor,
} from './button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-flat-button], a[app-flat-button]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FlatButtonComponent {
  readonly color = input<FlatButtonColor>('primary');
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(flatButtonVariants({ color: this.color() }), this.class());
  }
}
