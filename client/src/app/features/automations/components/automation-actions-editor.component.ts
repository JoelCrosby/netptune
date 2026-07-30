import { Component, input, output } from '@angular/core';
import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';
import { WorkspaceAppUser } from '@core/models/appuser';
import { RelationType } from '@core/models/relation-type';
import { Status } from '@core/models/status';
import { Tag } from '@core/models/tag';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import {
  LucideGripVertical,
  LucideListOrdered,
  LucideListPlus,
  LucideTrash2,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { actionTypeLabels } from '../models/automation-copy';
import {
  AutomationAction,
  AutomationActionType,
  AutomationDelayUnit,
} from '../models/automation.models';
import { AutomationCreateTaskEditorComponent } from './automation-create-task-editor.component';
import { AutomationNotifyEditorComponent } from './automation-notify-editor.component';
import { AutomationRelationEditorComponent } from './automation-relation-editor.component';
import { AutomationTaskUpdateEditorComponent } from './automation-task-update-editor.component';

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
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    BadgeComponent,
    PanelComponent,
    PanelHeaderComponent,
    LucideGripVertical,
    LucideListPlus,
    LucideTrash2,
    AutomationTaskUpdateEditorComponent,
    AutomationNotifyEditorComponent,
    AutomationCreateTaskEditorComponent,
    AutomationRelationEditorComponent,
  ],
  template: `
    <app-card-title
      i18n="Heading of the actions section — the 'then' part of the rule">
      Then
    </app-card-title>
    <app-card-subtitle>
      <span i18n="Explains that actions run in order">
        Add the follow-up actions in the order they should run.
      </span>
    </app-card-subtitle>

    <app-panel
      class="mt-4"
      i18n-aria-label="Accessible name of the follow-up actions panel"
      aria-label="Follow-up actions">
      <app-panel-header
        i18n-heading="Heading above the ordered list of actions"
        heading="Action sequence"
        i18n-description="Explains that automation actions run in order"
        description="Actions run from top to bottom."
        [icon]="actionSequenceIcon">
        <app-badge panelHeaderActions color="primary">
          {{ actions().length }} / 10
        </app-badge>
      </app-panel-header>

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
                    <span
                      i18n="
                        Heading of one action in the list. NUMBER is its
                        position
                      ">
                      Action
                      {{
                        actionIndex + 1 // i18n(ph="NUMBER")
                      }}
                    </span>
                  </span>
                </div>

                <button
                  app-icon-button
                  class="shrink-0"
                  color="warn"
                  type="button"
                  i18n-aria-label="
                    Accessible label for the button that removes an action
                  "
                  aria-label="Remove action"
                  i18n-title="Tooltip on the button that removes an action"
                  title="Remove action"
                  [disabled]="actions().length === 1"
                  (click)="removeAction.emit(action.clientId)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
              </header>

              <div class="flex min-w-0 flex-col gap-3 p-3">
                <app-form-select
                  i18n-label="Label of the action field"
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
                  <app-automation-notify-editor
                    [action]="action"
                    [users]="users()"
                    [ruleName]="ruleName()"
                    (patch)="
                      actionUpdated.emit({
                        clientId: action.clientId,
                        patch: $event,
                      })
                    " />
                } @else if (action.type === automationActionType.addComment) {
                  <app-form-textarea
                    i18n-label="Label of the comment field"
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
                      i18n-label="Label of the flag name field"
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
                      i18n-label="Label of the flag description field"
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
                } @else if (
                  action.type === automationActionType.manageTaskRelation
                ) {
                  <app-automation-relation-editor
                    [action]="action"
                    [relationTypes]="relationTypes()"
                    (patch)="
                      actionUpdated.emit({
                        clientId: action.clientId,
                        patch: $event,
                      })
                    " />
                } @else if (action.type === automationActionType.createTask) {
                  <app-automation-create-task-editor
                    [action]="action"
                    [statuses]="statuses()"
                    [users]="users()"
                    [tags]="tags()"
                    [sprints]="sprints()"
                    [boardGroups]="boardGroups()"
                    [relationTypes]="relationTypes()"
                    (patch)="
                      actionUpdated.emit({
                        clientId: action.clientId,
                        patch: $event,
                      })
                    " />
                } @else if (action.type === automationActionType.updateTask) {
                  <app-automation-task-update-editor
                    [action]="action"
                    [statuses]="statuses()"
                    [users]="users()"
                    [tags]="tags()"
                    [sprints]="sprints()"
                    [boardGroups]="boardGroups()"
                    [defaultStatusId]="defaultStatusId()"
                    (patch)="
                      actionUpdated.emit({
                        clientId: action.clientId,
                        patch: $event,
                      })
                    " />
                } @else if (action.type === automationActionType.deleteTask) {
                  <div class="grid gap-3 md:grid-cols-2">
                    <app-form-input
                      i18n-label="Label of the delay field"
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
                      i18n-label="Label of the unit field"
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
                        <span i18n="Delay unit: minutes">Minutes</span>
                      </app-form-select-option>
                      <app-form-select-option
                        [value]="automationDelayUnit.hours">
                        <span i18n="Delay unit: hours">Hours</span>
                      </app-form-select-option>
                      <app-form-select-option
                        [value]="automationDelayUnit.days">
                        <span i18n="Delay unit: days">Days</span>
                      </app-form-select-option>
                    </app-form-select>
                  </div>
                  <p class="text-sm text-red-600 dark:text-red-400">
                    <span i18n="Caveat on the delete-task action">
                      The task will only be deleted if its status has not
                      changed during the delay. Deleted tasks can be restored
                      from the archive.
                    </span>
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
                <span i18n="Button that adds another automation action">
                  Add action
                </span>
              </button>
            </div>
          }
        }
      </div>
    </app-panel>
  `,
})
export class AutomationActionsEditorComponent {
  readonly actionSequenceIcon = LucideListOrdered;
  readonly automationActionType = AutomationActionType;
  readonly automationDelayUnit = AutomationDelayUnit;
  readonly actionTypes = [
    AutomationActionType.notifyTaskAssignees,
    AutomationActionType.flagTask,
    AutomationActionType.updateTask,
    AutomationActionType.addComment,
    AutomationActionType.deleteTask,
    AutomationActionType.createTask,
    AutomationActionType.manageTaskRelation,
  ];
  readonly actions = input.required<EditableAutomationAction[]>();
  readonly statuses = input.required<Status[]>();
  readonly users = input.required<WorkspaceAppUser[]>();
  readonly ruleName = input('');
  readonly tags = input.required<Tag[]>();
  readonly sprints = input.required<SprintViewModel[]>();
  readonly boardGroups = input.required<AutomationBoardGroupOption[]>();
  readonly relationTypes = input.required<RelationType[]>();
  readonly defaultStatusId = input<number | null>(null);

  readonly addAction = output();
  readonly removeAction = output<number>();
  readonly actionTypeChanged = output<AutomationActionTypeChange>();
  readonly actionUpdated = output<AutomationActionUpdate>();

  actionTypeLabel(type: AutomationActionType): string {
    return actionTypeLabels[type];
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
