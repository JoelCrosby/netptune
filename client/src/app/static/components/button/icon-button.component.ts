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
  type IconButtonSize,
} from './button.variants';
import { TooltipDirective } from '../../directives/tooltip.directive';

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
  private readonly tooltip = inject(TooltipDirective, {
    self: true,
    optional: true,
  });

  readonly color = input<IconButtonColor>('default');
  readonly size = input<IconButtonSize>('default');
  readonly class = input('');
  readonly ariaLabel = input<string | null>(null);

  @HostBinding('attr.aria-label') get accessibleName(): string | null {
    return (
      this.ariaLabel() ??
      this.hostAriaLabel ??
      this.hostTitle ??
      (this.tooltip?.appTooltip() || null)
    );
  }

  @HostBinding('class') get className(): string {
    return cn(
      iconButtonVariants({
        color: coerceIconButtonColor(this.color()),
        size: this.size(),
      }),
      this.class()
    );
  }
}
