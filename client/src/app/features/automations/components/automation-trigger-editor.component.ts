import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import {
  taskStatusLabels,
  taskStatusOptions,
  TaskStatus,
} from '@core/enums/project-task-status';
import { CardComponent } from '@static/components/card/card.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { triggerTypeLabels } from '../models/automation-copy';
import { AutomationTriggerType } from '../models/automation.models';

@Component({
  selector: 'app-automation-trigger-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
  template: `
    <app-card class="min-h-0! p-5!">
      <div class="flex flex-col gap-4">
        <div>
          <h2 class="text-lg font-semibold">When</h2>
          <p class="text-muted text-sm">
            Choose the task event this automation watches.
          </p>
        </div>

        <app-form-select label="Trigger" [(value)]="triggerType">
          @for (type of triggerTypes; track type) {
            <app-form-select-option [value]="type">
              {{ triggerTypeLabel(type) }}
            </app-form-select-option>
          }
        </app-form-select>

        @if (triggerType() === automationTriggerType.taskStatusChanged) {
          <div class="flex flex-wrap items-end gap-3">
            <span class="pb-7 text-sm">Task status changes to</span>
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
  readonly triggerTypes = [
    AutomationTriggerType.taskStatusChanged,
    AutomationTriggerType.taskUnassignedFor,
  ];
  readonly statusOptions = taskStatusOptions;

  readonly triggerType = model<AutomationTriggerType>(
    AutomationTriggerType.taskStatusChanged
  );
  readonly status = model<TaskStatus>(TaskStatus.complete);
  readonly durationDays = model('3');

  triggerTypeLabel(type: AutomationTriggerType): string {
    return triggerTypeLabels[type];
  }

  statusLabel(status: TaskStatus): string {
    return taskStatusLabels[status];
  }
}
