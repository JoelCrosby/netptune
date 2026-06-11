import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import {
  TaskStatus,
  taskStatusLabels,
  taskStatusOptions,
} from '@core/enums/project-task-status';
import { TaskPriority, taskPriorityOptions } from '@core/enums/task-priority';
import { LucidePlus, LucideTrash2 } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { actionTypeLabels } from '../models/automation-copy';
import {
  AutomationAction,
  AutomationActionType,
} from '../models/automation.models';

export interface EditableAutomationAction extends AutomationAction {
  clientId: number;
}

export interface AutomationActionTypeChange {
  clientId: number;
  type: AutomationActionType;
}

export interface AutomationActionUpdate {
  clientId: number;
  patch: Partial<EditableAutomationAction>;
}

@Component({
  selector: 'app-automation-actions-editor',
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
    FormTextAreaComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    LucidePlus,
    LucideTrash2,
  ],
  template: `
    <app-card>
      <app-card-header>
        <app-card-title>Then</app-card-title>
        <app-card-subtitle>
          Add the follow-up actions in the order they should run.
        </app-card-subtitle>
      </app-card-header>

      <div class="flex flex-col gap-4">
        <div class="flex items-start justify-between gap-3">
          <button
            app-stroked-button
            type="button"
            [disabled]="actions().length >= 10"
            (click)="addAction.emit()">
            <svg lucidePlus class="h-4 w-4"></svg>
            Add Action
          </button>
        </div>

        <div class="flex flex-col gap-3">
          @for (action of actions(); track action.clientId) {
            <div class="border-border rounded border p-4">
              <div class="flex flex-col-reverse gap-3">
                <div class="flex flex-col gap-3">
                  <app-form-select
                    label="Action"
                    [value]="action.type"
                    (changed)="
                      actionTypeChanged.emit({
                        clientId: action.clientId,
                        type: $event,
                      })
                    ">
                    @for (type of actionTypes; track type) {
                      <app-form-select-option [value]="type">
                        {{ actionTypeLabel(type) }}
                      </app-form-select-option>
                    }
                  </app-form-select>

                  @if (
                    action.type === automationActionType.notifyTaskAssignees
                  ) {
                    <app-form-textarea
                      label="Message"
                      rows="3"
                      [value]="action.message ?? ''"
                      (valueChange)="
                        actionUpdated.emit({
                          clientId: action.clientId,
                          patch: { message: $event },
                        })
                      " />
                  } @else if (action.type === automationActionType.flagTask) {
                    <div class="grid gap-3 md:grid-cols-2">
                      <app-form-input
                        label="Flag name"
                        [required]="true"
                        [value]="action.flagName ?? ''"
                        (valueChange)="
                          actionUpdated.emit({
                            clientId: action.clientId,
                            patch: { flagName: $event },
                          })
                        " />
                      <app-form-input
                        label="Flag description"
                        [value]="action.flagDescription ?? ''"
                        (valueChange)="
                          actionUpdated.emit({
                            clientId: action.clientId,
                            patch: { flagDescription: $event },
                          })
                        " />
                    </div>
                  } @else {
                    <div class="grid gap-4 md:grid-cols-2">
                      <div class="flex flex-col gap-3">
                        <app-checkbox
                          [checked]="hasStatusUpdate(action)"
                          (changed)="
                            actionUpdated.emit({
                              clientId: action.clientId,
                              patch: {
                                status: $event ? defaultTaskStatus : null,
                              },
                            })
                          ">
                          Set status
                        </app-checkbox>
                        @if (hasStatusUpdate(action)) {
                          <app-form-select
                            label="Status"
                            [value]="action.status ?? null"
                            (changed)="
                              actionUpdated.emit({
                                clientId: action.clientId,
                                patch: { status: $event },
                              })
                            ">
                            @for (status of taskStatuses; track status) {
                              <app-form-select-option [value]="status">
                                {{ taskStatusLabel(status) }}
                              </app-form-select-option>
                            }
                          </app-form-select>
                        }
                      </div>
                      <div class="flex flex-col gap-3">
                        <app-checkbox
                          [checked]="hasPriorityUpdate(action)"
                          (changed)="
                            actionUpdated.emit({
                              clientId: action.clientId,
                              patch: {
                                priority: $event ? defaultTaskPriority : null,
                              },
                            })
                          ">
                          Set priority
                        </app-checkbox>
                        @if (hasPriorityUpdate(action)) {
                          <app-form-select
                            label="Priority"
                            [value]="action.priority ?? null"
                            (changed)="
                              actionUpdated.emit({
                                clientId: action.clientId,
                                patch: { priority: $event },
                              })
                            ">
                            @for (
                              priority of taskPriorities;
                              track priority.value
                            ) {
                              <app-form-select-option [value]="priority.value">
                                {{ priority.label }}
                              </app-form-select-option>
                            }
                          </app-form-select>
                        }
                      </div>
                    </div>
                  }
                </div>

                <button
                  app-icon-button
                  type="button"
                  title="Remove action"
                  class="ml-auto"
                  [disabled]="actions().length === 1"
                  (click)="removeAction.emit(action.clientId)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
              </div>
            </div>
          }
        </div>
      </div>
    </app-card>
  `,
})
export class AutomationActionsEditorComponent {
  readonly automationActionType = AutomationActionType;
  readonly actionTypes = [
    AutomationActionType.notifyTaskAssignees,
    AutomationActionType.flagTask,
    AutomationActionType.updateTask,
  ];
  readonly taskStatuses = taskStatusOptions;
  readonly taskPriorities = taskPriorityOptions;
  readonly defaultTaskStatus = TaskStatus.inProgress;
  readonly defaultTaskPriority = TaskPriority.none;

  readonly actions = input.required<EditableAutomationAction[]>();

  readonly addAction = output();
  readonly removeAction = output<number>();
  readonly actionTypeChanged = output<AutomationActionTypeChange>();
  readonly actionUpdated = output<AutomationActionUpdate>();

  actionTypeLabel(type: AutomationActionType): string {
    return actionTypeLabels[type];
  }

  taskStatusLabel(status: (typeof taskStatusOptions)[number]): string {
    return taskStatusLabels[status];
  }

  hasStatusUpdate(action: AutomationAction): boolean {
    return action.status !== null && action.status !== undefined;
  }

  hasPriorityUpdate(action: AutomationAction): boolean {
    return action.priority !== null && action.priority !== undefined;
  }
}
