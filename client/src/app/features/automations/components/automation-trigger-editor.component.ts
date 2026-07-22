import { Component, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import {
  taskChangeFieldLabels,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  AutomationConditionOperator,
  AutomationFieldCondition,
  AutomationTriggerType,
  TaskChangeField,
} from '../models/automation.models';
import { AutomationFieldConditionEditorComponent } from './automation-field-condition-editor.component';

@Component({
  selector: 'app-automation-trigger-editor',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    AutomationFieldConditionEditorComponent,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <app-form-select label="Trigger" [(value)]="triggerType">
        @for (type of triggerTypes; track type) {
          <app-form-select-option [value]="type">
            {{ triggerTypeLabel(type) }}
          </app-form-select-option>
        }
      </app-form-select>

      @if (triggerType() === automationTriggerType.taskChanged) {
        <div class="flex flex-col gap-3">
          <span class="text-sm font-medium">Fields</span>
          <div class="grid gap-3 sm:grid-cols-2">
            @for (field of taskFieldOptions; track field) {
              <app-checkbox
                [checked]="hasTaskField(field)"
                (changed)="toggleTaskField(field, $event)">
                {{ taskFieldLabel(field) }}
              </app-checkbox>
            }
          </div>
        </div>

        @if (taskFields().length) {
          <div class="border-border flex flex-col gap-3 border-t pt-4">
            <span class="text-sm font-medium">Conditions</span>
            @for (field of taskFields(); track field) {
              <app-automation-field-condition-editor
                [field]="field"
                [statuses]="statuses()"
                [condition]="conditionFor(field)"
                (conditionChange)="setCondition($event)" />
            }
          </div>
        }
      } @else if (triggerType() === automationTriggerType.taskUnassignedFor) {
        <div class="flex flex-wrap items-end gap-3">
          <div class="w-48">
            <app-form-input
              label="Task is unassigned for "
              name="durationDays"
              type="number"
              [required]="true"
              [(value)]="durationDays" />
          </div>
          <span class="pb-7 text-sm">days</span>
        </div>
      } @else {
        <div class="flex flex-wrap items-end gap-3">
          <div class="w-32">
            <app-form-input
              label="Lead time"
              name="durationDays"
              type="number"
              [required]="true"
              [(value)]="durationDays" />
          </div>
          <span class="pb-7 text-sm">days before the due date</span>
        </div>
      }
    </div>
  `,
})
export class AutomationTriggerEditorComponent {
  readonly automationTriggerType = AutomationTriggerType;
  readonly triggerTypes = [
    AutomationTriggerType.taskChanged,
    AutomationTriggerType.taskUnassignedFor,
    AutomationTriggerType.taskDueDateApproaching,
  ];
  readonly taskFieldOptions = [
    TaskChangeField.name,
    TaskChangeField.description,
    TaskChangeField.status,
    TaskChangeField.assignees,
    TaskChangeField.priority,
    TaskChangeField.estimate,
    TaskChangeField.dueDate,
    TaskChangeField.tags,
    TaskChangeField.startDate,
  ];
  readonly statuses = input.required<Status[]>();

  readonly triggerType = model<AutomationTriggerType>(
    AutomationTriggerType.taskChanged
  );
  readonly taskFields = model<TaskChangeField[]>([TaskChangeField.status]);
  readonly conditions = model<AutomationFieldCondition[]>([]);
  readonly durationDays = model('3');

  triggerTypeLabel(type: AutomationTriggerType): string {
    return triggerTypeLabels[type];
  }

  taskFieldLabel(field: TaskChangeField): string {
    return taskChangeFieldLabels[field];
  }

  hasTaskField(field: TaskChangeField): boolean {
    return this.taskFields().includes(field);
  }

  toggleTaskField(field: TaskChangeField, checked: boolean) {
    const fields = this.taskFields();

    this.taskFields.set(
      checked
        ? [...new Set([...fields, field])]
        : fields.filter((selected) => selected !== field)
    );

    if (checked && !this.conditions().some((item) => item.field === field)) {
      this.conditions.update((conditions) => [
        ...conditions,
        { field, operator: AutomationConditionOperator.any, value: null },
      ]);
    } else if (!checked) {
      this.conditions.update((conditions) =>
        conditions.filter((condition) => condition.field !== field)
      );
    }
  }

  conditionFor(field: TaskChangeField): AutomationFieldCondition {
    return (
      this.conditions().find((condition) => condition.field === field) ?? {
        field,
        operator: AutomationConditionOperator.any,
        value: null,
      }
    );
  }

  setCondition(condition: AutomationFieldCondition) {
    this.conditions.update((conditions) => [
      ...conditions.filter((item) => item.field !== condition.field),
      condition,
    ]);
  }
}
