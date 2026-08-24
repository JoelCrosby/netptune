import { Component, computed, input, model } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import {
  findQueryField,
  findQueryOperator,
  newQueryCondition,
  operatorValueCount,
  QueryBuilderCatalog,
  QueryBuilderCondition,
  QueryBuilderField,
  QueryBuilderOperator,
} from './query-builder.models';

@Component({
  selector: 'app-query-condition-editor',
  imports: [
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
    LucideDynamicIcon,
  ],
  template: `
    <div
      class="grid min-w-0 flex-1 gap-2 lg:grid-cols-[minmax(150px,1fr)_minmax(150px,1fr)_minmax(180px,1.4fr)]">
      <app-form-select
        i18n-label="Label of the query builder field picker"
        label="Field"
        [noMargin]="true"
        [value]="condition().field"
        (valueChange)="setField($event)">
        @for (option of catalog().fields; track option.key) {
          <app-form-select-option [value]="option.key">
            {{ option.name }}
          </app-form-select-option>
        }
      </app-form-select>

      <app-form-select
        i18n-label="Label of the query builder operator picker"
        label="Condition"
        [noMargin]="true"
        [value]="condition().operator"
        (valueChange)="setOperator($event)">
        @for (option of operatorOptions(); track option.key) {
          <app-form-select-option [value]="option.key">
            @if (option.icon) {
              <span class="flex items-center gap-2">
                <svg
                  [lucideIcon]="option.icon"
                  class="text-primary h-4 w-4 shrink-0"
                  aria-hidden="true"></svg>
                <span>{{ option.label }}</span>
              </span>
            } @else {
              {{ option.label }}
            }
          </app-form-select-option>
        }
      </app-form-select>

      @if (takesNoValue()) {
        <div class="hidden lg:block"></div>
      } @else if (takesManyValues()) {
        <app-form-select-tags
          i18n-label="Label of the query builder value picker"
          label="Value"
          i18n-placeholder="Placeholder text: Choose one or more values"
          placeholder="Choose one or more values"
          [value]="condition().values"
          (changed)="setValues($event)">
          @for (option of valueOptions(); track option.value) {
            <app-form-select-tags-option [value]="option.value">
              {{ option.label }}
            </app-form-select-tags-option>
          }
        </app-form-select-tags>
      } @else if (valueOptions().length) {
        <app-form-select
          [label]="valueLabel()"
          [noMargin]="true"
          [value]="valueAt(0)"
          (valueChange)="setValueAt(0, $event)">
          @for (option of valueOptions(); track option.value) {
            <app-form-select-option [value]="option.value">
              {{ option.label }}
            </app-form-select-option>
          }
        </app-form-select>
      } @else if (takesRange()) {
        <div class="grid grid-cols-2 gap-2">
          <app-form-input
            i18n-label="Label of the lower end of a query builder range"
            label="From"
            [name]="inputName() + '-from'"
            [type]="inputType()"
            [noMargin]="true"
            [value]="valueAt(0)"
            (valueChange)="setValueAt(0, $event)" />
          <app-form-input
            i18n-label="Label of the upper end of a query builder range"
            label="To"
            [name]="inputName() + '-to'"
            [type]="inputType()"
            [noMargin]="true"
            [value]="valueAt(1)"
            (valueChange)="setValueAt(1, $event)" />
        </div>
      } @else {
        <app-form-input
          [label]="valueLabel()"
          [name]="inputName()"
          [type]="inputType()"
          [noMargin]="true"
          [placeholder]="valuePlaceholder()"
          [value]="valueAt(0)"
          (valueChange)="setValueAt(0, $event)" />
      }
    </div>
  `,
})
export class QueryConditionEditorComponent {
  readonly catalog = input.required<QueryBuilderCatalog>();
  readonly condition = model.required<QueryBuilderCondition>();

  readonly field = computed<QueryBuilderField | undefined>(() => {
    return findQueryField(this.catalog(), this.condition().field);
  });

  readonly operator = computed<QueryBuilderOperator | undefined>(() => {
    return findQueryOperator(this.field(), this.condition().operator);
  });

  readonly operatorOptions = computed(() => this.field()?.operators ?? []);

  readonly valueOptions = computed(() => this.field()?.options ?? []);

  readonly takesNoValue = computed(() => this.valueCount() === 0);

  readonly takesManyValues = computed(() => !!this.operator()?.acceptsMany);

  readonly takesRange = computed(() => this.valueCount() === 2);

  readonly inputName = computed(() => `query-${this.condition().field}`);

  readonly inputType = computed(() => {
    return this.operator()?.inputType ?? this.field()?.inputType ?? 'text';
  });

  readonly valueLabel = computed(() => {
    return (
      this.operator()?.valueLabel ??
      $localize`:Label of the query builder value picker:Value`
    );
  });

  readonly valuePlaceholder = computed(() => {
    return (
      this.operator()?.valuePlaceholder ?? this.field()?.valuePlaceholder ?? ''
    );
  });

  valueAt(index: number): string {
    return this.condition().values[index] ?? '';
  }

  setField(fieldKey: string | null) {
    if (fieldKey === null) return;

    const field = findQueryField(this.catalog(), fieldKey);

    if (!field) return;

    this.condition.set(newQueryCondition(field));
  }

  setOperator(operatorKey: string | null) {
    if (operatorKey === null) return;

    const operator = findQueryOperator(this.field(), operatorKey);

    this.condition.update((condition) => ({
      ...condition,
      operator: operatorKey,
      values: carryValues(condition.values, operator),
    }));
  }

  setValues(values: string[]) {
    this.condition.update((condition) => ({ ...condition, values }));
  }

  setValueAt(index: number, value: string | null) {
    const limit = valueLimit(this.operator());

    this.condition.update((condition) => {
      const values = [...condition.values];

      values[index] = value ?? '';

      return { ...condition, values: values.slice(0, limit) };
    });
  }

  private valueCount(): number {
    return operatorValueCount(this.operator());
  }
}

function valueLimit(operator: QueryBuilderOperator | undefined): number {
  return operator?.acceptsMany
    ? Number.MAX_SAFE_INTEGER
    : operatorValueCount(operator);
}

function carryValues(
  values: string[],
  operator: QueryBuilderOperator | undefined
): string[] {
  const arity = operatorValueCount(operator);

  if (arity === 0) return [];

  if (operator?.acceptsMany) return values;

  return values.slice(0, arity);
}
