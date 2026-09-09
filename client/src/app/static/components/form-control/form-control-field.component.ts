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
import {
  FormControlDensity,
  FormControlShapeDirective,
} from './form-control.directives';

const disabledBackground =
  'color-mix(in oklab, var(--foreground) 2%, var(--form-field-background))';

@Component({
  selector: 'app-form-control-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <ng-content /> `,
  host: {
    '[class]': 'hostClass()',
    '[style.background]': 'background()',
    '[style.borderColor]': 'borderColor()',
    '(focusin)': 'focused.set(true)',
    '(focusout)': 'focused.set(false)',
  },
})
export class FormControlFieldComponent {
  readonly invalid = input(false, { transform: (value: unknown) => !!value });
  readonly disabled = input(false, { transform: (value: unknown) => !!value });
  readonly active = input(false, { transform: (value: unknown) => !!value });
  readonly density = input<FormControlDensity>('default');
  readonly class = input('');

  // Read from an ancestor rather than an input of its own, so a whole form opts in at once.
  private readonly shapeSource = inject(FormControlShapeDirective, {
    optional: true,
  });

  protected readonly hostClass = computed(() => {
    const base =
      'flex w-[inherit] max-w-[inherit] flex-row items-center bg-form-field-background transition-colors duration-200 ease-out';
    const rounded = this.shapeSource?.appFormShape() === 'rounded';
    const shape =
      this.density() === 'compact'
        ? 'h-[38px] rounded-lg border'
        : `${rounded ? 'rounded-lg' : 'rounded-sm'} border-2`;

    return cn(base, shape, this.class());
  });

  readonly el: HTMLElement = inject(ElementRef).nativeElement;

  protected readonly focused = signal(false);

  // The whole surface has to carry the disabled tint, because the control inside stops short of the
  // chevron and any suffix, which would leave those strips at the enabled colour.
  protected readonly background = computed(() => {
    return this.disabled() ? disabledBackground : null;
  });

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
