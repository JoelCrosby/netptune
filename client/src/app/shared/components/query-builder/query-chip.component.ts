import {
  Component,
  computed,
  ElementRef,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { LucideTrash2, LucideX } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import {
  findQueryField,
  findQueryOperator,
  operatorValueCount,
  QueryBuilderCatalog,
  QueryBuilderCondition,
  QueryBuilderField,
  queryOptionLabel,
} from './query-builder.models';
import { QueryConditionEditorComponent } from './query-condition-editor.component';

@Component({
  selector: 'app-query-chip',
  imports: [
    DropdownMenuComponent,
    FlatButtonComponent,
    QueryConditionEditorComponent,
    LucideTrash2,
    LucideX,
  ],
  host: { class: 'relative inline-flex' },
  template: `
    <div
      class="border-border bg-foreground/5 flex h-9 items-center overflow-hidden rounded-[9px] border transition-colors"
      [class.border-primary/60]="menu.showing()"
      [class.bg-primary/10]="menu.showing()"
      [class.border-warn/70]="invalid() && !menu.showing()"
      [class.bg-warn/10]="invalid() && !menu.showing()">
      <button
        #origin
        type="button"
        class="hover:bg-foreground/5 flex h-full items-center gap-[7px] px-3 text-sm whitespace-nowrap transition-colors"
        [attr.aria-expanded]="menu.showing()"
        [attr.aria-label]="ariaLabel()"
        (click)="menu.toggle(origin)">
        <span class="text-foreground font-medium">{{ fieldName() }}</span>
        <span class="text-foreground/45">{{ operatorLabel() }}</span>
        @if (valueLabel()) {
          <span class="text-primary font-medium">{{ valueLabel() }}</span>
        }
      </button>

      <button
        type="button"
        class="text-foreground/35 hover:text-warn hover:bg-warn/10 border-border flex h-full w-[30px] items-center justify-center border-l transition-colors"
        i18n-aria-label="
          Accessible label for the button that removes a condition
        "
        aria-label="Remove condition"
        (click)="removed.emit()">
        <svg lucideX class="h-3.5 w-3.5"></svg>
      </button>
    </div>

    <app-dropdown-menu panelRole="dialog" #menu>
      <div class="w-[420px] max-w-[calc(100vw-2rem)] p-3">
        <app-query-condition-editor
          [catalog]="catalog()"
          [condition]="condition()"
          (conditionChange)="condition.set($event)" />

        <div class="mt-3 flex items-center justify-between gap-2">
          <button
            app-flat-button
            color="ghost"
            class="text-warn h-8 gap-2 px-2.5 text-[13px]"
            type="button"
            (click)="removed.emit(); menu.close()">
            <svg lucideTrash2 class="h-3.5 w-3.5"></svg>
            <span i18n="Button that removes a condition">Remove</span>
          </button>

          <button
            app-flat-button
            color="neutral"
            class="h-8 px-3.5 text-[13px]"
            type="button"
            (click)="menu.close()">
            <span i18n="Button that closes a popover">Done</span>
          </button>
        </div>
      </div>
    </app-dropdown-menu>
  `,
})
export class QueryChipComponent {
  readonly catalog = input.required<QueryBuilderCatalog>();
  readonly invalid = input(false);
  readonly condition = model.required<QueryBuilderCondition>();
  readonly removed = output();

  readonly origin = viewChild.required<ElementRef<HTMLElement>>('origin');

  readonly field = computed<QueryBuilderField | undefined>(() => {
    return findQueryField(this.catalog(), this.condition().field);
  });

  readonly fieldName = computed(
    () => this.field()?.name ?? this.condition().field
  );

  readonly operator = computed(() => {
    return findQueryOperator(this.field(), this.condition().operator);
  });

  readonly operatorLabel = computed(() => this.operator()?.label ?? '');

  readonly valueLabel = computed(() => {
    const condition = this.condition();
    const operator = this.operator();
    const arity = operatorValueCount(operator);

    if (arity === 0) return '';

    const labels = condition.values.map((value) =>
      queryOptionLabel(this.field(), value)
    );

    if (arity === 2) {
      return `${labels[0] ?? '…'} – ${labels[1] ?? '…'}`;
    }

    if (operator?.acceptsMany) {
      // Three names in a chip is already long; the rest are counted rather than listed.
      return labels.length > 3
        ? $localize`:Chip summary of several picked values:${labels
            .slice(0, 2)
            .join(', ')}:values: +${labels.length - 2}:overflowCount:`
        : labels.join(', ');
    }

    const value = labels[0] || '…';

    return operator?.valueSuffix ? `${value} ${operator.valueSuffix}` : value;
  });

  readonly ariaLabel = computed(() => {
    return `${this.fieldName()} ${this.operatorLabel()} ${this.valueLabel()}`.trim();
  });
}
