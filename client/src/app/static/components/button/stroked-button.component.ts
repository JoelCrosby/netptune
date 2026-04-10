import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-stroked-button], a[app-stroked-button]',
  template: '<ng-content />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrokedButtonComponent {
  readonly color = input<'primary' | 'warn'>('primary');

  @HostBinding('class') get className(): string {
    const base =
      'inline-flex items-center justify-center gap-2 px-4 h-9 min-w-16 rounded-sm text-sm font-medium tracking-wide cursor-pointer select-none transition-colors border focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50';

    const colors: Record<string, string> = {
      primary:
        'border-border text-primary bg-transparent hover:bg-primary/10 focus-visible:ring-primary',
      warn: 'border-warn text-warn bg-transparent hover:bg-warn/10 focus-visible:ring-warn',
    };

    return `${base} ${colors[this.color()]}`;
  }
}
