import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  signal,
} from '@angular/core';

@Component({
  selector: 'app-form-control-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-content />`,
  host: {
    class:
      'flex w-[inherit] max-w-[inherit] flex-row items-center rounded-sm border-2 bg-form-field-background transition-colors duration-200 ease-out',
    '[style.borderColor]': 'borderColor()',
    '(focusin)': 'focused.set(true)',
    '(focusout)': 'focused.set(false)',
  },
})
export class FormControlFieldComponent {
  readonly invalid = input(false, { transform: (value: unknown) => !!value });
  readonly active = input(false, { transform: (value: unknown) => !!value });

  readonly el: HTMLElement = inject(ElementRef).nativeElement;

  protected readonly focused = signal(false);

  protected readonly borderColor = computed(() => {
    if (this.invalid()) {
      return 'var(--warn)';
    }

    if (this.active() || this.focused()) {
      return 'var(--primary)';
    }

    return 'color-mix(in oklab, var(--foreground) 30%, transparent)';
  });
}
