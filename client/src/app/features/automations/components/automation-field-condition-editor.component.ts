import { Component, input, model } from '@angular/core';
import { Status } from '@core/models/status';
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

@Component({
  selector: 'app-automation-field-condition-editor',
  imports: [FormInputComponent, FormSelectComponent, FormSelectOptionComponent],
  template: `
    <div class="grid gap-3 sm:grid-cols-2">
      <app-form-select
        [label]="fieldLabel()"
        [value]="condition().operator"
        (valueChange)="setOperator($event)">
        @for (option of operatorOptions(); track option.value) {
          <app-form-select-option [value]="option.value">
            {{ option.label }}
          </app-form-select-option>
        }
      </app-form-select>

      @if (requiresValue()) {
        @if (field() === taskChangeField.status) {
          <app-form-select
            label="Value"
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
            label="Value"
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
            label="Value"
            [name]="'condition-' + field()"
            [type]="isDateField() ? 'date' : 'text'"
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
  readonly condition = model.required<AutomationFieldCondition>();

  fieldLabel(): string {
    return taskChangeFieldLabels[this.field()];
  }

  operatorOptions(): { label: string; value: AutomationConditionOperator }[] {
    if (this.isCollectionField()) {
      return [
        { label: 'Any change', value: AutomationConditionOperator.any },
        { label: 'Added', value: AutomationConditionOperator.added },
        { label: 'Removed', value: AutomationConditionOperator.removed },
      ];
    }

    const options = [
      { label: 'Any change', value: AutomationConditionOperator.any },
      { label: 'Equals', value: AutomationConditionOperator.equals },
      { label: 'Does not equal', value: AutomationConditionOperator.notEquals },
    ];

    if (this.isTextField()) {
      options.push({
        label: 'Contains',
        value: AutomationConditionOperator.contains,
      });
    }

    options.push(
      { label: 'Is empty', value: AutomationConditionOperator.isEmpty },
      { label: 'Is not empty', value: AutomationConditionOperator.isNotEmpty }
    );

    return options;
  }

  valueOptions(): SelectOption[] {
    if (this.field() === TaskChangeField.priority) {
      return [
        { label: 'None', value: 'None' },
        { label: 'Low', value: 'Low' },
        { label: 'Medium', value: 'Medium' },
        { label: 'High', value: 'High' },
        { label: 'Critical', value: 'Critical' },
      ];
    }

    if (this.field() === TaskChangeField.estimate) {
      return [
        { label: 'Story points', value: 'StoryPoints' },
        { label: 'Hours', value: 'Hours' },
        { label: 'T-shirt', value: 'TShirt' },
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
      this.field() === TaskChangeField.tags &&
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
    return this.field() === TaskChangeField.tags ? 'Any tag' : '';
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
      (this.field() === TaskChangeField.tags &&
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
