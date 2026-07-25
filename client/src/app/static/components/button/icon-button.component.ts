import {
  Component,
  HostAttributeToken,
  HostBinding,
  inject,
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
})
export class IconButtonComponent {
  private readonly hostAriaLabel = inject(
    new HostAttributeToken('aria-label'),
    {
      optional: true,
    }
  );
  private readonly hostTitle = inject(new HostAttributeToken('title'), {
    optional: true,
  });

  readonly color = input<IconButtonColor>('default');
  readonly class = input('');
  readonly ariaLabel = input<string | null>(null);

  @HostBinding('attr.aria-label') get accessibleName(): string | null {
    return this.ariaLabel() ?? this.hostAriaLabel ?? this.hostTitle;
  }

  @HostBinding('class') get className(): string {
    return cn(
      iconButtonVariants({ color: coerceIconButtonColor(this.color()) }),
      this.class()
    );
  }
}
