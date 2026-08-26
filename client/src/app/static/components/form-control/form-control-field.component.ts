import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { cn } from '../button/button.variants';
import { FormControlDensity } from './form-control.directives';

@Component({
  selector: 'app-form-control-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <ng-content /> `,
  host: {
    '[class]': 'hostClass()',
    '[style.borderColor]': 'borderColor()',
    '(focusin)': 'focused.set(true)',
    '(focusout)': 'focused.set(false)',
  },
})
export class FormControlFieldComponent {
  readonly invalid = input(false, { transform: (value: unknown) => !!value });
  readonly active = input(false, { transform: (value: unknown) => !!value });
  readonly density = input<FormControlDensity>('default');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    const base =
      'flex w-[inherit] max-w-[inherit] flex-row items-center bg-form-field-background transition-colors duration-200 ease-out';
    const shape =
      this.density() === 'compact'
        ? 'h-[38px] rounded-lg border'
        : 'rounded-sm border-2';

    return cn(base, shape, this.class());
  });

  readonly el: HTMLElement = inject(ElementRef).nativeElement;

  protected readonly focused = signal(false);

  protected readonly borderColor = computed(() => {
    if (this.invalid()) {
      return 'var(--warn)';
    }

    if (this.active() || this.focused()) {
      return 'var(--primary)';
    }

    const idleOpacity = this.density() === 'compact' ? 15 : 30;

    return `color-mix(in oklab, var(--foreground) ${idleOpacity}%, transparent)`;
  });
}
