import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-icon-button]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconButtonComponent {
  readonly color = input<'primary' | 'warn' | 'default'>('default');

  @HostBinding('class') get className(): string {
    const base =
      'inline-flex items-center justify-center h-10 w-10 rounded-full cursor-pointer select-none transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50';

    const colors: Record<string, string> = {
      primary: 'text-primary hover:bg-primary/10 focus-visible:ring-primary',
      warn: 'text-warn hover:bg-warn/10 focus-visible:ring-warn',
      default:
        'text-foreground hover:bg-foreground/10 focus-visible:ring-foreground',
    };

    return `${base} ${colors[this.color()]}`;
  }
}
