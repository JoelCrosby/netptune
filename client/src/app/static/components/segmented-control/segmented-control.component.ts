import { Component, input, model } from '@angular/core';

export interface SegmentedOption<T extends string = string> {
  value: T;
  label: string;
}

@Component({
  selector: 'app-segmented-control',
  host: {
    class: 'bg-hover inline-flex rounded-full p-0.5 text-xs',
    role: 'group',
    '[attr.aria-label]': 'ariaLabel()',
  },
  template: `
    @for (option of options(); track option.value) {
      <button
        type="button"
        class="rounded-full px-3 py-1.5 transition-colors"
        [class]="
          option.value === value()
            ? 'bg-card text-foreground shadow-sm'
            : 'text-muted hover:text-foreground'
        "
        [attr.aria-pressed]="option.value === value()"
        (click)="value.set(option.value)">
        {{ option.label }}
      </button>
    }
  `,
})
export class SegmentedControlComponent<T extends string = string> {
  readonly options = input.required<SegmentedOption<T>[]>();
  readonly ariaLabel = input<string | null>(null);

  readonly value = model.required<T>();
}
