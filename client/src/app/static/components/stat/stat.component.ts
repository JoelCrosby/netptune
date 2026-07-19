import { booleanAttribute, Component, computed, input } from '@angular/core';

export type StatValue = number | string;

@Component({
  selector: 'app-stat',
  template: `
    <div [class]="containerClass()">
      <span [class]="labelClass()">
        {{ label() }}
      </span>
      <strong [class]="valueClass()">{{ value() }}</strong>
    </div>
  `,
})
export class StatComponent {
  readonly label = input.required<string>();
  readonly value = input.required<StatValue>();
  readonly compact = input(false, { transform: booleanAttribute });

  protected readonly containerClass = computed(
    () =>
      'border-border bg-card-header flex flex-col items-start justify-center rounded border shadow-sm ' +
      (this.compact() ? 'p-3' : 'min-h-24 p-6')
  );

  protected readonly labelClass = computed(
    () =>
      'text-muted font-medium uppercase ' +
      (this.compact() ? 'text-[0.7rem]' : 'text-xs')
  );

  protected readonly valueClass = computed(() =>
    this.compact() ? 'text-lg font-semibold' : 'mt-1 text-2xl'
  );
}
