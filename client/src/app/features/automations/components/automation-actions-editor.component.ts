import { Component, input, output } from '@angular/core';
import { TaskPriority, taskPriorityOptions } from '@core/enums/task-priority';
import { Status } from '@core/models/status';
import {
  LucideGripVertical,
  LucideListOrdered,
  LucideListPlus,
  LucideTrash2,
} from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
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
  AutomationDelayUnit,
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
  imports: [
    CardSubtitleComponent,
    CardTitleComponent,
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    LucideGripVertical,
    LucideListOrdered,
    LucideListPlus,
    LucideTrash2,
  ],
  template: `
    <app-card-title>Then</app-card-title>
    <app-card-subtitle>
      Add the follow-up actions in the order they should run.
    </app-card-subtitle>

    <section
      class="border-border bg-background mt-4 overflow-hidden rounded-lg border shadow-sm"
      aria-label="Follow-up actions">
      <div
        class="border-border bg-foreground/3 flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
        <div class="flex items-center gap-3">
          <span
            class="bg-primary/10 text-primary flex h-8 w-8 shrink-0 items-center justify-center rounded-full">
            <svg lucideListOrdered class="h-4 w-4" aria-hidden="true"></svg>
          </span>
          <div>
            <p class="text-sm font-medium">Action sequence</p>
            <p class="text-foreground/60 text-xs">
              Actions run from top to bottom.
            </p>
          </div>
        </div>
        <span
          class="bg-primary/10 text-primary rounded-full px-2.5 py-1 text-xs font-semibold">
          {{ actions().length }} / 10
        </span>
      </div>

      <div class="flex flex-col p-3 sm:p-4">
        @for (
          action of actions();
          track action.clientId;
          let actionIndex = $index;
          let lastAction = $last
        ) {
          <div class="flex min-w-0 gap-2 sm:gap-3">
            <div
              class="relative flex w-8 shrink-0 justify-center sm:w-10"
              aria-hidden="true">
              @if (!lastAction) {
                <div
                  class="bg-primary/30 absolute top-8 bottom-0 left-1/2 w-px"></div>
              }
              <span
                class="bg-primary text-primary-foreground relative flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold shadow-sm">
                {{ actionIndex + 1 }}
              </span>
            </div>

            <article
              class="border-border bg-background mb-3 min-w-0 flex-1 overflow-hidden rounded-lg border shadow-xs">
              <header
                class="border-border bg-foreground/3 flex items-center justify-between gap-2 border-b px-3 py-2">
                <div class="flex min-w-0 items-center gap-2">
                  <svg
                    lucideGripVertical
                    class="text-foreground/35 h-4 w-4 shrink-0"
                    aria-hidden="true"></svg>
                  <span class="truncate text-sm font-semibold">
                    Action {{ actionIndex + 1 }}
                  </span>
                </div>

                <button
                  app-icon-button
                  class="shrink-0"
                  color="warn"
                  type="button"
                  aria-label="Remove action"
                  title="Remove action"
                  [disabled]="actions().length === 1"
                  (click)="removeAction.emit(action.clientId)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
              </header>

              <div class="flex min-w-0 flex-col gap-3 p-3">
                <app-form-select
                  label="Action"
                  [noMargin]="true"
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

                @if (action.type === automationActionType.notifyTaskAssignees) {
                  <app-form-textarea
                    label="Message"
                    rows="3"
                    [noMargin]="true"
                    [value]="action.message ?? ''"
                    (valueChange)="
                      actionUpdated.emit({
                        clientId: action.clientId,
                        patch: { message: $event },
                      })
                    " />
                } @else if (action.type === automationActionType.addComment) {
                  <app-form-textarea
                    label="Comment"
                    rows="3"
                    [noMargin]="true"
                    [maxLength]="32768"
                    [value]="action.comment ?? ''"
                    (valueChange)="
                      actionUpdated.emit({
                        clientId: action.clientId,
                        patch: { comment: $event },
                      })
                    " />
                } @else if (action.type === automationActionType.flagTask) {
                  <div class="grid gap-3 md:grid-cols-2">
                    <app-form-input
                      label="Flag name"
                      [required]="true"
                      [noMargin]="true"
                      [value]="action.flagName ?? ''"
                      (valueChange)="
                        actionUpdated.emit({
                          clientId: action.clientId,
                          patch: { flagName: $event },
                        })
                      " />
                    <app-form-input
                      label="Flag description"
                      [noMargin]="true"
                      [value]="action.flagDescription ?? ''"
                      (valueChange)="
                        actionUpdated.emit({
                          clientId: action.clientId,
                          patch: { flagDescription: $event },
                        })
                      " />
                  </div>
                } @else if (action.type === automationActionType.updateTask) {
                  <div class="grid gap-4 md:grid-cols-2">
                    <div class="flex flex-col gap-3">
                      <app-checkbox
                        [checked]="hasStatusUpdate(action)"
                        (changed)="
                          actionUpdated.emit({
                            clientId: action.clientId,
                            patch: {
                              statusId: $event ? defaultStatusId() : null,
                            },
                          })
                        ">
                        Set status
                      </app-checkbox>
                      @if (hasStatusUpdate(action)) {
                        <app-form-select
                          label="Status"
                          [noMargin]="true"
                          [value]="action.statusId ?? null"
                          (changed)="
                            actionUpdated.emit({
                              clientId: action.clientId,
                              patch: { statusId: $event },
                            })
                          ">
                          @for (status of statuses(); track status.id) {
                            <app-form-select-option [value]="status.id">
                              {{ status.name }}
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
                          [noMargin]="true"
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
                } @else if (action.type === automationActionType.deleteTask) {
                  <div class="grid gap-3 md:grid-cols-2">
                    <app-form-input
                      label="Delay"
                      type="number"
                      min="0"
                      max="525600"
                      [noMargin]="true"
                      [value]="delayAmountValue(action)"
                      (valueChange)="
                        actionUpdated.emit({
                          clientId: action.clientId,
                          patch: { delayAmount: parseDelayAmount($event) },
                        })
                      " />
                    <app-form-select
                      label="Unit"
                      [noMargin]="true"
                      [value]="action.delayUnit ?? automationDelayUnit.minutes"
                      (changed)="
                        actionUpdated.emit({
                          clientId: action.clientId,
                          patch: { delayUnit: $event },
                        })
                      ">
                      <app-form-select-option
                        [value]="automationDelayUnit.minutes">
                        Minutes
                      </app-form-select-option>
                      <app-form-select-option
                        [value]="automationDelayUnit.hours">
                        Hours
                      </app-form-select-option>
                      <app-form-select-option
                        [value]="automationDelayUnit.days">
                        Days
                      </app-form-select-option>
                    </app-form-select>
                  </div>
                  <p class="text-sm text-red-600 dark:text-red-400">
                    The task will only be deleted if its status has not changed
                    during the delay. Deleted tasks can be restored from the
                    archive.
                  </p>
                }
              </div>
            </article>
          </div>

          @if (lastAction) {
            <div class="flex min-w-0 gap-2 sm:gap-3">
              <div class="flex w-8 shrink-0 justify-center sm:w-10">
                <div
                  class="border-primary/40 text-primary flex h-8 w-8 items-center justify-center rounded-full border border-dashed">
                  <svg lucideListPlus class="h-4 w-4"></svg>
                </div>
              </div>
              <button
                app-stroked-button
                class="gap-2 self-start"
                type="button"
                [disabled]="actions().length >= 10"
                (click)="addAction.emit()">
                <svg lucideListPlus class="h-4 w-4"></svg>
                Add action
              </button>
            </div>
          }
        }
      </div>
    </section>
  `,
})
export class AutomationActionsEditorComponent {
  readonly automationActionType = AutomationActionType;
  readonly automationDelayUnit = AutomationDelayUnit;
  readonly actionTypes = [
    AutomationActionType.notifyTaskAssignees,
    AutomationActionType.flagTask,
    AutomationActionType.updateTask,
    AutomationActionType.addComment,
    AutomationActionType.deleteTask,
  ];
  readonly taskPriorities = taskPriorityOptions;
  readonly defaultTaskPriority = TaskPriority.none;

  readonly actions = input.required<EditableAutomationAction[]>();
  readonly statuses = input.required<Status[]>();
  readonly defaultStatusId = input<number | null>(null);

  readonly addAction = output();
  readonly removeAction = output<number>();
  readonly actionTypeChanged = output<AutomationActionTypeChange>();
  readonly actionUpdated = output<AutomationActionUpdate>();

  actionTypeLabel(type: AutomationActionType): string {
    return actionTypeLabels[type];
  }

  hasStatusUpdate(action: AutomationAction): boolean {
    return action.statusId !== null && action.statusId !== undefined;
  }

  hasPriorityUpdate(action: AutomationAction): boolean {
    return action.priority !== null && action.priority !== undefined;
  }

  parseDelayAmount(value: string): number | null {
    if (!value.trim()) return null;

    const amount = Number(value);

    return Number.isInteger(amount) ? amount : null;
  }

  delayAmountValue(action: AutomationAction): string {
    return String(action.delayAmount ?? 0);
  }
}
