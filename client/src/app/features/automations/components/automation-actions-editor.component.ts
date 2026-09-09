import { Component, input, output } from '@angular/core';
import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';
import { WorkspaceAppUser } from '@core/models/appuser';
import { RelationType } from '@core/models/relation-type';
import { Status } from '@core/models/status';
import { Tag } from '@core/models/tag';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { LucidePlus, LucideTrash2 } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
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
import { AutomationCreateTaskEditorComponent } from './automation-create-task-editor.component';
import { AutomationFlowCardComponent } from './automation-flow-card.component';
import { AutomationFlowStepComponent } from './automation-flow-step.component';
import { AutomationNotifyEditorComponent } from './automation-notify-editor.component';
import { AutomationRelationEditorComponent } from './automation-relation-editor.component';
import { AutomationTaskUpdateEditorComponent } from './automation-task-update-editor.component';

export const automationActionLimit = 10;

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
    AutomationFlowCardComponent,
    AutomationFlowStepComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    LucidePlus,
    LucideTrash2,
    AutomationTaskUpdateEditorComponent,
    AutomationNotifyEditorComponent,
    AutomationCreateTaskEditorComponent,
    AutomationRelationEditorComponent,
  ],
  host: { class: 'flex flex-col' },
  template: `
    @for (action of actions(); track action.clientId; let index = $index) {
      <app-automation-flow-step [step]="index + 1">
        <app-automation-flow-card [keyword]="stepKeyword(index)">
          <div flowCardHeader class="w-full max-w-75 min-w-0">
            <label
              class="sr-only"
              [for]="'action-type-' + action.clientId"
              i18n="Label of the action field">
              Action
            </label>
            <app-form-select
              [name]="'action-type-' + action.clientId"
              label=""
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
          </div>

          <button
            flowCardActions
            app-icon-button
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
            <div class="grid gap-3.5 md:grid-cols-2">
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
          } @else if (action.type === automationActionType.manageTaskRelation) {
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
            <div class="grid gap-3.5 md:grid-cols-2">
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
                <app-form-select-option [value]="automationDelayUnit.minutes">
                  <span i18n="Delay unit: minutes">Minutes</span>
                </app-form-select-option>
                <app-form-select-option [value]="automationDelayUnit.hours">
                  <span i18n="Delay unit: hours">Hours</span>
                </app-form-select-option>
                <app-form-select-option [value]="automationDelayUnit.days">
                  <span i18n="Delay unit: days">Days</span>
                </app-form-select-option>
              </app-form-select>
            </div>

            <p
              class="bg-warn/8 text-warn rounded-lg px-3 py-2.5 text-[13px] leading-normal text-pretty">
              <span i18n="Caveat on the delete-task action">
                The task is only deleted if its status has not changed during
                the delay. Deleted tasks can be restored from the archive.
              </span>
            </p>
          }
        </app-automation-flow-card>
      </app-automation-flow-step>
    }

    <app-automation-flow-step
      [icon]="addActionIcon"
      appearance="dashed"
      [connected]="false">
      <div class="flex h-8 items-center gap-3.5">
        <button
          app-stroked-button
          class="gap-2"
          type="button"
          [disabled]="atActionLimit()"
          (click)="addAction.emit()">
          <svg lucidePlus class="h-3.5 w-3.5"></svg>
          <span i18n="Button that adds another automation action">
            Add action
          </span>
        </button>

        <span class="text-foreground/50 text-[13px]">
          <span
            i18n="
              How many automation actions are in use. USED is that count, LIMIT
              the maximum
            ">
            {{
              actions().length // i18n(ph="USED")
            }}
            of
            {{
              actionLimit // i18n(ph="LIMIT")
            }}
            actions used
          </span>
        </span>
      </div>
    </app-automation-flow-step>
  `,
})
export class AutomationActionsEditorComponent {
  readonly addActionIcon = LucidePlus;
  readonly actionLimit = automationActionLimit;
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

  stepKeyword(index: number): string {
    return index === 0
      ? $localize`:Heading of the first action in the rule:THEN`
      : $localize`:Heading of a follow-on action in the rule:AND THEN`;
  }

  atActionLimit(): boolean {
    return this.actions().length >= automationActionLimit;
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
