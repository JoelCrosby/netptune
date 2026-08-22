import { Component, computed, inject, input, model } from '@angular/core';
import {
  acceptsManyValues,
  isRelativeDayOperator,
  operatorArity,
  taskQueryOperatorLabels,
} from '@app/features/task-views/models/task-query-copy';
import {
  TaskQueryCatalog,
  TaskQueryCondition,
  TaskQueryField,
  TaskQueryOperator,
  TaskQueryValueType,
} from '@app/features/task-views/models/task-view.models';
import { QueryFieldOptionsService } from '@app/features/task-views/services/query-field-options.service';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';

@Component({
  selector: 'app-query-condition-editor',
  imports: [
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
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
        @for (option of operatorOptions(); track option) {
          <app-form-select-option [value]="option">
            {{ operatorLabel(option) }}
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
          i18n-label="Label of the query builder value picker"
          label="Value"
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
  private readonly fieldOptions = inject(QueryFieldOptionsService);

  readonly catalog = input.required<TaskQueryCatalog>();
  readonly condition = model.required<TaskQueryCondition>();

  readonly field = computed<TaskQueryField | undefined>(() => {
    return this.catalog().fields.find(
      (candidate) => candidate.key === this.condition().field
    );
  });

  readonly operatorOptions = computed(() => this.field()?.operators ?? []);

  readonly valueOptions = computed(() => {
    return this.fieldOptions.optionsFor(this.field());
  });

  readonly takesNoValue = computed(() => {
    return operatorArity(this.condition().operator) === 0;
  });

  readonly takesManyValues = computed(() => {
    return acceptsManyValues(this.condition().operator);
  });

  readonly takesRange = computed(() => {
    return this.condition().operator === TaskQueryOperator.between;
  });

  readonly inputName = computed(() => `query-${this.condition().field}`);

  readonly inputType = computed(() => {
    if (isRelativeDayOperator(this.condition().operator)) return 'number';

    const valueType = this.field()?.valueType;
    const isDate =
      valueType === TaskQueryValueType.date ||
      valueType === TaskQueryValueType.timestamp;

    if (isDate) return 'date';

    return valueType === TaskQueryValueType.number ? 'number' : 'text';
  });

  readonly valueLabel = computed(() => {
    if (isRelativeDayOperator(this.condition().operator)) {
      return $localize`:Label of the day-count field in the query builder:Days`;
    }

    return $localize`:Label of the query builder value picker:Value`;
  });

  readonly valuePlaceholder = computed(() => {
    if (isRelativeDayOperator(this.condition().operator)) return '7';

    return '';
  });

  operatorLabel(operator: TaskQueryOperator): string {
    return taskQueryOperatorLabels[operator];
  }

  valueAt(index: number): string {
    return this.condition().values[index] ?? '';
  }

  setField(fieldKey: string | null) {
    if (fieldKey === null) return;

    const field = this.catalog().fields.find(
      (candidate) => candidate.key === fieldKey
    );
    const operator = field?.operators[0] ?? TaskQueryOperator.equals;

    this.condition.set({ field: fieldKey, operator, values: [] });
  }

  setOperator(operator: TaskQueryOperator | null) {
    if (operator === null) return;

    this.condition.update((condition) => ({
      ...condition,
      operator,
      values: carryValues(condition.values, operator),
    }));
  }

  setValues(values: string[]) {
    this.condition.update((condition) => ({ ...condition, values }));
  }

  setValueAt(index: number, value: string | null) {
    this.condition.update((condition) => {
      const values = [...condition.values];

      values[index] = value ?? '';

      return {
        ...condition,
        values: values.slice(0, arityFor(condition.operator)),
      };
    });
  }
}

function arityFor(operator: TaskQueryOperator): number {
  return acceptsManyValues(operator)
    ? Number.MAX_SAFE_INTEGER
    : operatorArity(operator);
}

function carryValues(values: string[], operator: TaskQueryOperator): string[] {
  const arity = operatorArity(operator);

  if (arity === 0) return [];

  if (acceptsManyValues(operator)) return values;

  return values.slice(0, arity);
}
