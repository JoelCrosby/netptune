import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import {
  taskStatusLabels,
  taskStatusOptions,
  TaskStatus,
} from '@core/enums/project-task-status';
import { CardComponent } from '@static/components/card/card.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import {
  assigneeChangeModeLabels,
  taskChangeFieldLabels,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  AutomationTriggerType,
  AssigneeChangeMode,
  TaskChangeField,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-trigger-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
  template: `
    <app-card class="min-h-0! p-5!">
      <app-card-header>
        <app-card-title>When</app-card-title>
        <app-card-subtitle>
          Choose the task event this automation watches.
        </app-card-subtitle>
      </app-card-header>

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

          @if (hasTaskField(taskChangeField.status)) {
            <div class="flex flex-wrap items-end gap-3">
              <span class="pb-7 text-sm">Status changes to</span>
              <div class="min-w-64 flex-1">
                <app-form-select label="Status" [(value)]="status">
                  @for (option of statusOptions; track option) {
                    <app-form-select-option [value]="option">
                      {{ statusLabel(option) }}
                    </app-form-select-option>
                  }
                </app-form-select>
              </div>
            </div>
          }

          @if (hasTaskField(taskChangeField.assignees)) {
            <div class="flex flex-wrap items-end gap-3">
              <span class="pb-7 text-sm">Assignees are</span>
              <div class="min-w-64 flex-1">
                <app-form-select
                  label="Assignee change"
                  [(value)]="assigneeChangeMode">
                  @for (option of assigneeChangeModeOptions; track option) {
                    <app-form-select-option [value]="option">
                      {{ assigneeChangeModeLabel(option) }}
                    </app-form-select-option>
                  }
                </app-form-select>
              </div>
            </div>
          }
        } @else {
          <div class="flex flex-wrap items-end gap-3">
            <span class="pb-7 text-sm">Task is unassigned for</span>
            <div class="w-32">
              <app-form-input
                label="Days"
                name="durationDays"
                type="number"
                [required]="true"
                [(value)]="durationDays" />
            </div>
            <span class="pb-7 text-sm">days</span>
          </div>
        }
      </div>
    </app-card>
  `,
})
export class AutomationTriggerEditorComponent {
  readonly automationTriggerType = AutomationTriggerType;
  readonly taskChangeField = TaskChangeField;
  readonly triggerTypes = [
    AutomationTriggerType.taskChanged,
    AutomationTriggerType.taskUnassignedFor,
  ];
  readonly taskFieldOptions = [
    TaskChangeField.name,
    TaskChangeField.description,
    TaskChangeField.status,
    TaskChangeField.assignees,
    TaskChangeField.owner,
    TaskChangeField.priority,
    TaskChangeField.estimate,
  ];
  readonly statusOptions = taskStatusOptions;
  readonly assigneeChangeModeOptions = [
    AssigneeChangeMode.addedOrRemoved,
    AssigneeChangeMode.added,
    AssigneeChangeMode.removed,
  ];

  readonly triggerType = model<AutomationTriggerType>(
    AutomationTriggerType.taskChanged
  );
  readonly taskFields = model<TaskChangeField[]>([TaskChangeField.status]);
  readonly status = model<TaskStatus>(TaskStatus.complete);
  readonly assigneeChangeMode = model<AssigneeChangeMode>(
    AssigneeChangeMode.addedOrRemoved
  );
  readonly durationDays = model('3');

  triggerTypeLabel(type: AutomationTriggerType): string {
    return triggerTypeLabels[type];
  }

  statusLabel(status: TaskStatus): string {
    return taskStatusLabels[status];
  }

  taskFieldLabel(field: TaskChangeField): string {
    return taskChangeFieldLabels[field];
  }

  assigneeChangeModeLabel(mode: AssigneeChangeMode): string {
    return assigneeChangeModeLabels[mode];
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
  }
}
