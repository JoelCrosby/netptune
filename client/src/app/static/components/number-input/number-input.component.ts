import { Component, computed, input, model } from '@angular/core';
import { LucideMinus, LucidePlus } from '@lucide/angular';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import {
  FormControlDensity,
  FormControlInputDirective,
} from '@static/components/form-control/form-control.directives';

@Component({
  selector: 'app-number-input',
  imports: [
    FormControlFieldComponent,
    FormControlInputDirective,
    LucideMinus,
    LucidePlus,
  ],
  host: {
    class: 'block',
  },
  template: `
    <app-form-control-field [density]="density()" [invalid]="invalid()">
      <button
        type="button"
        [class]="stepperClass('left')"
        i18n-aria-label="Accessible label for the button that lowers a number"
        aria-label="Decrease"
        [disabled]="disabled() || !canDecrement()"
        (click)="stepBy(-1)">
        <svg lucideMinus class="h-4 w-4" aria-hidden="true"></svg>
      </button>

      <input
        appFormInput
        type="number"
        class="min-w-0 flex-1 [appearance:textfield] px-1 text-center leading-none [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        [id]="name() || null"
        [value]="value()"
        [disabled]="disabled()"
        [attr.min]="min()"
        [attr.max]="max()"
        [attr.step]="step()"
        [attr.placeholder]="placeholder()"
        [attr.aria-label]="ariaLabel()"
        (input)="onInput($event)"
        (blur)="onBlur($event)" />

      <button
        type="button"
        [class]="stepperClass('right')"
        i18n-aria-label="Accessible label for the button that raises a number"
        aria-label="Increase"
        [disabled]="disabled() || !canIncrement()"
        (click)="stepBy(1)">
        <svg lucidePlus class="h-4 w-4" aria-hidden="true"></svg>
      </button>
    </app-form-control-field>
  `,
})
export class NumberInputComponent {
  readonly value = model<number | null>(null);
  readonly touched = model(false);
  readonly name = input<string>('');
  readonly min = input<number | null>(null);
  readonly max = input<number | null>(null);
  readonly step = input(1);
  readonly placeholder = input<string | null>('—');
  readonly ariaLabel = input<string | null>(null);
  readonly disabled = input(false);
  readonly invalid = input(false);
  readonly density = input<FormControlDensity>('compact');

  protected stepperClass(side: 'left' | 'right') {
    const base =
      'text-foreground/70 hover:text-foreground hover:bg-foreground/8 focus-visible:ring-primary flex h-full w-9 shrink-0 cursor-pointer items-center justify-center transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-40';
    const compact = this.density() === 'compact';

    if (side === 'left') {
      return compact ? `${base} rounded-l-lg` : `${base} rounded-l-sm`;
    }

    return compact ? `${base} rounded-r-lg` : `${base} rounded-r-sm`;
  }

  protected readonly canDecrement = computed(() => {
    const min = this.min();

    return min === null || (this.value() ?? min) > min;
  });

  protected readonly canIncrement = computed(() => {
    const max = this.max();

    return max === null || (this.value() ?? max) < max;
  });

  protected onInput(event: Event) {
    const input = event.target as HTMLInputElement;

    if (!input.value) {
      this.value.set(null);

      return;
    }

    const parsed = input.valueAsNumber;

    this.value.set(isNaN(parsed) ? null : parsed);
  }

  // Clamping while typing fights the caret, so out-of-range entries are corrected on blur.
  protected onBlur(event: Event) {
    this.touched.set(true);

    const value = this.value();
    const clamped = value === null ? null : this.clamp(value);

    if (clamped === value) return;

    const input = event.target as HTMLInputElement;

    input.value = clamped === null ? '' : `${clamped}`;
    this.value.set(clamped);
  }

  protected stepBy(direction: number) {
    const start = this.value() ?? this.min() ?? 0;

    this.value.set(this.clamp(start + direction * this.step()));
  }

  private clamp(value: number): number | null {
    if (isNaN(value)) return null;

    const min = this.min();
    const max = this.max();
    const lowerBounded = min === null ? value : Math.max(min, value);

    return max === null ? lowerBounded : Math.min(max, lowerBounded);
  }
}
