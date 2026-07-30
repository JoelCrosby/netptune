import { Component, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import {
  LucideAsterisk,
  LucideCircleDashed,
  LucideCircleDot,
  LucideDynamicIcon,
  LucideEqual,
  LucideEqualNot,
  LucideIconInput,
  LucideMinus,
  LucidePlus,
  LucideTextSearch,
} from '@lucide/angular';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { taskChangeFieldLabels } from '../models/automation-copy';
import {
  AutomationConditionOperator,
  AutomationFieldCondition,
  TaskChangeField,
} from '../models/automation.models';

interface SelectOption {
  label: string;
  value: string;
}

interface OperatorOption {
  icon: LucideIconInput;
  label: string;
  value: AutomationConditionOperator;
}

@Component({
  selector: 'app-automation-field-condition-editor',
  imports: [
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    LucideDynamicIcon,
  ],
  template: `
    <div class="grid min-w-0 gap-2 sm:grid-cols-2">
      <app-form-select
        [label]="fieldLabel()"
        [noMargin]="true"
        [value]="condition().operator"
        (valueChange)="setOperator($event)">
        @for (option of operatorOptions(); track option.value) {
          <app-form-select-option [value]="option.value">
            <span class="flex items-center gap-2">
              <svg
                [lucideIcon]="option.icon"
                class="text-primary h-4 w-4 shrink-0"
                aria-hidden="true"></svg>
              <span>{{ option.label }}</span>
            </span>
          </app-form-select-option>
        }
      </app-form-select>

      @if (requiresValue()) {
        @if (field() === taskChangeField.status) {
          <app-form-select
            i18n-label="Label of the value field"
            label="Value"
            [noMargin]="true"
            [value]="condition().value ?? null"
            (valueChange)="setValue($event)">
            @for (status of statuses(); track status.id) {
              <app-form-select-option [value]="status.id.toString()">
                {{ status.name }}
              </app-form-select-option>
            }
          </app-form-select>
        } @else if (valueOptions().length) {
          <app-form-select
            i18n-label="Label of the value field"
            label="Value"
            [noMargin]="true"
            [value]="condition().value ?? null"
            (valueChange)="setValue($event)">
            @for (option of valueOptions(); track option.value) {
              <app-form-select-option [value]="option.value">
                {{ option.label }}
              </app-form-select-option>
            }
          </app-form-select>
        } @else {
          <app-form-input
            i18n-label="Label of the value field"
            label="Value"
            [name]="'condition-' + field()"
            [type]="isDateField() ? 'date' : 'text'"
            [noMargin]="true"
            [placeholder]="valuePlaceholder()"
            [value]="condition().value ?? ''"
            (valueChange)="setValue($event)" />
        }
      }
    </div>
  `,
})
export class AutomationFieldConditionEditorComponent {
  readonly taskChangeField = TaskChangeField;
  readonly field = input.required<TaskChangeField>();
  readonly statuses = input.required<Status[]>();
  readonly supportsChangeOperators = input(false);
  readonly operatorLabel = input<string | null>(null);
  readonly condition = model.required<AutomationFieldCondition>();

  fieldLabel(): string {
    return this.operatorLabel() ?? taskChangeFieldLabels[this.field()];
  }

  operatorOptions(): OperatorOption[] {
    if (this.isCollectionField()) {
      const options = [
        {
          icon: LucideAsterisk,
          label: $localize`:Condition operator matching any change:Any change`,
          value: AutomationConditionOperator.any,
        },
        {
          icon: LucideEqual,
          label: $localize`:Condition operator matching when a value is present:Includes`,
          value: AutomationConditionOperator.equals,
        },
        {
          icon: LucideEqualNot,
          label: $localize`:Condition operator matching when a value is absent:Does not include`,
          value: AutomationConditionOperator.notEquals,
        },
        {
          icon: LucideTextSearch,
          label: $localize`:Condition operator matching a substring:Contains text`,
          value: AutomationConditionOperator.contains,
        },
        {
          icon: LucideCircleDashed,
          label: $localize`:Condition operator matching an empty field:Is empty`,
          value: AutomationConditionOperator.isEmpty,
        },
        {
          icon: LucideCircleDot,
          label: $localize`:Condition operator matching a non-empty field:Is not empty`,
          value: AutomationConditionOperator.isNotEmpty,
        },
        {
          icon: LucidePlus,
          label: $localize`:Condition operator matching an added value:Added`,
          value: AutomationConditionOperator.added,
        },
        {
          icon: LucideMinus,
          label: $localize`:Condition operator matching a removed value:Removed`,
          value: AutomationConditionOperator.removed,
        },
      ];

      return this.supportsChangeOperators()
        ? options
        : options.filter(
            (option) =>
              option.value !== AutomationConditionOperator.any &&
              option.value !== AutomationConditionOperator.added &&
              option.value !== AutomationConditionOperator.removed
          );
    }

    const options: OperatorOption[] = [
      {
        icon: LucideAsterisk,
        label: $localize`:Condition operator matching any change:Any change`,
        value: AutomationConditionOperator.any,
      },
      {
        icon: LucideEqual,
        label: $localize`:Condition operator matching an exact value:Equals`,
        value: AutomationConditionOperator.equals,
      },
      {
        icon: LucideEqualNot,
        label: $localize`:Condition operator matching anything but a value:Does not equal`,
        value: AutomationConditionOperator.notEquals,
      },
    ];

    if (this.isTextField()) {
      options.push({
        icon: LucideTextSearch,
        label: $localize`:Condition operator matching a substring:Contains`,
        value: AutomationConditionOperator.contains,
      });
    }

    options.push(
      {
        icon: LucideCircleDashed,
        label: $localize`:Condition operator matching an empty field:Is empty`,
        value: AutomationConditionOperator.isEmpty,
      },
      {
        icon: LucideCircleDot,
        label: $localize`:Condition operator matching a non-empty field:Is not empty`,
        value: AutomationConditionOperator.isNotEmpty,
      }
    );

    return this.supportsChangeOperators()
      ? options
      : options.filter(
          (option) => option.value !== AutomationConditionOperator.any
        );
  }

  valueOptions(): SelectOption[] {
    if (this.field() === TaskChangeField.priority) {
      return [
        { label: $localize`:Task priority level, none:None`, value: 'None' },
        { label: $localize`:Task priority level, low:Low`, value: 'Low' },
        {
          label: $localize`:Task priority level, medium:Medium`,
          value: 'Medium',
        },
        { label: $localize`:Task priority level, high:High`, value: 'High' },
        {
          label: $localize`:Task priority level, critical:Critical`,
          value: 'Critical',
        },
      ];
    }

    if (this.field() === TaskChangeField.estimate) {
      return [
        {
          label: $localize`:Estimation unit, story points:Story points`,
          value: 'StoryPoints',
        },
        { label: $localize`:Estimation unit, hours:Hours`, value: 'Hours' },
        {
          label: $localize`:Estimation unit, t-shirt sizes:T-shirt`,
          value: 'TShirt',
        },
      ];
    }

    return [];
  }

  requiresValue(): boolean {
    const operator = this.condition().operator;

    if (
      operator === AutomationConditionOperator.equals ||
      operator === AutomationConditionOperator.notEquals ||
      operator === AutomationConditionOperator.contains
    ) {
      return true;
    }

    return (
      this.isCollectionField() &&
      (operator === AutomationConditionOperator.added ||
        operator === AutomationConditionOperator.removed)
    );
  }

  isDateField(): boolean {
    return (
      this.field() === TaskChangeField.startDate ||
      this.field() === TaskChangeField.dueDate
    );
  }

  valuePlaceholder(): string {
    if (this.field() === TaskChangeField.tags) return 'Tag name';
    if (this.field() === TaskChangeField.assignees) return 'User ID';

    return '';
  }

  setOperator(operator: AutomationConditionOperator | null) {
    if (operator === null) return;

    this.condition.update((condition) => ({
      ...condition,
      operator,
      value: this.operatorUsesValue(operator) ? condition.value : null,
    }));
  }

  setValue(value: string | null) {
    this.condition.update((condition) => ({ ...condition, value }));
  }

  private operatorUsesValue(operator: AutomationConditionOperator): boolean {
    return (
      operator === AutomationConditionOperator.equals ||
      operator === AutomationConditionOperator.notEquals ||
      operator === AutomationConditionOperator.contains ||
      (this.isCollectionField() &&
        (operator === AutomationConditionOperator.added ||
          operator === AutomationConditionOperator.removed))
    );
  }

  private isCollectionField(): boolean {
    return (
      this.field() === TaskChangeField.assignees ||
      this.field() === TaskChangeField.tags
    );
  }

  private isTextField(): boolean {
    return (
      this.field() === TaskChangeField.name ||
      this.field() === TaskChangeField.description
    );
  }
}
