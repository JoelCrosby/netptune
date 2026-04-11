import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-flat-button], a[app-flat-button]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FlatButtonComponent {
  readonly color = input<'primary' | 'warn' | 'ghost'>('primary');

  @HostBinding('class') get className(): string {
    const base =
      'inline-flex items-center justify-center gap-2 px-4 h-10 min-w-16 rounded-sm text-sm font-medium tracking-wide cursor-pointer select-none transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50';

    const colors: Record<string, string> = {
      primary:
        'bg-primary text-white dark:text-neutral-900 hover:bg-primary/90 focus-visible:ring-primary',
      warn: 'bg-warn text-white dark:text-neutral-900 hover:bg-warn/90 focus-visible:ring-warn',
      ghost:
        'bg-transparent text-foreground hover:bg-foreground/10 active:bg-foreground/20 focus-visible:ring-warn',
    };

    return `${base} ${colors[this.color()]}`;
  }
}
