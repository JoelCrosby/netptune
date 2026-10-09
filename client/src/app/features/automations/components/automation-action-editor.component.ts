import { Component, input, output } from '@angular/core';
import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';
import { WorkspaceAppUser } from '@core/models/appuser';
import { RelationType } from '@core/models/relation-type';
import { Status } from '@core/models/status';
import { Tag } from '@core/models/tag';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { LucideTrash2 } from '@lucide/angular';
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
import { AutomationNotifyEditorComponent } from './automation-notify-editor.component';
import { AutomationRelationEditorComponent } from './automation-relation-editor.component';
import { AutomationTaskUpdateEditorComponent } from './automation-task-update-editor.component';

export const automationActionLimit = 10;

export interface EditableAutomationAction extends AutomationAction {
  clientId: number;
}

@Component({
  selector: 'app-automation-action-editor',
  imports: [
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    LucideTrash2,
    AutomationTaskUpdateEditorComponent,
    AutomationNotifyEditorComponent,
    AutomationCreateTaskEditorComponent,
    AutomationRelationEditorComponent,
  ],
  host: { class: 'flex flex-col gap-4.5' },
  template: `
    <app-form-select
      name="action-type"
      i18n-label="Label of the action field"
      label="Action"
      [noMargin]="true"
      [value]="action().type"
      (changed)="typeChanged.emit($event)">
      @for (type of actionTypes; track type) {
        <app-form-select-option [value]="type">
          {{ actionTypeLabel(type) }}
        </app-form-select-option>
      }
    </app-form-select>

    @if (action().type === automationActionType.notifyTaskAssignees) {
      <app-automation-notify-editor
        [action]="action()"
        [users]="users()"
        [ruleName]="ruleName()"
        (patch)="patch.emit($event)" />
    } @else if (action().type === automationActionType.addComment) {
      <app-form-textarea
        i18n-label="Label of the comment field"
        label="Comment"
        rows="3"
        [noMargin]="true"
        [maxLength]="32768"
        [value]="action().comment ?? ''"
        (valueChange)="patch.emit({ comment: $event })" />
    } @else if (action().type === automationActionType.flagTask) {
      <div class="grid gap-3.5 md:grid-cols-2">
        <app-form-input
          i18n-label="Label of the flag name field"
          label="Flag name"
          [required]="true"
          [noMargin]="true"
          [value]="action().flagName ?? ''"
          (valueChange)="patch.emit({ flagName: $event })" />
        <app-form-input
          i18n-label="Label of the flag description field"
          label="Flag description"
          [noMargin]="true"
          [value]="action().flagDescription ?? ''"
          (valueChange)="patch.emit({ flagDescription: $event })" />
      </div>
    } @else if (action().type === automationActionType.manageTaskRelation) {
      <app-automation-relation-editor
        [action]="action()"
        [relationTypes]="relationTypes()"
        (patch)="patch.emit($event)" />
    } @else if (action().type === automationActionType.createTask) {
      <app-automation-create-task-editor
        [action]="action()"
        [statuses]="statuses()"
        [users]="users()"
        [tags]="tags()"
        [sprints]="sprints()"
        [boardGroups]="boardGroups()"
        [relationTypes]="relationTypes()"
        (patch)="patch.emit($event)" />
    } @else if (action().type === automationActionType.updateTask) {
      <app-automation-task-update-editor
        [action]="action()"
        [statuses]="statuses()"
        [users]="users()"
        [tags]="tags()"
        [sprints]="sprints()"
        [boardGroups]="boardGroups()"
        [defaultStatusId]="defaultStatusId()"
        (patch)="patch.emit($event)" />
    } @else if (action().type === automationActionType.deleteTask) {
      <div class="grid gap-3.5 md:grid-cols-2">
        <app-form-input
          i18n-label="Label of the delay field"
          label="Delay"
          type="number"
          min="0"
          max="525600"
          [noMargin]="true"
          [value]="delayAmountValue(action())"
          (valueChange)="
            patch.emit({ delayAmount: parseDelayAmount($event) })
          " />
        <app-form-select
          i18n-label="Label of the unit field"
          label="Unit"
          [noMargin]="true"
          [value]="action().delayUnit ?? automationDelayUnit.minutes"
          (changed)="patch.emit({ delayUnit: $event })">
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
          The task is only deleted if its status has not changed during the
          delay. Deleted tasks can be restored from the archive.
        </span>
      </p>
    }

    @if (canRemove()) {
      <button
        type="button"
        class="text-warn hover:bg-foreground/5 focus-visible:ring-primary -ml-2.5 inline-flex h-8 items-center gap-1.5 self-start rounded-md px-2.5 text-sm font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none"
        (click)="remove.emit()">
        <svg lucideTrash2 class="h-3.5 w-3.5"></svg>
        <span i18n="Button that removes an action">Remove action</span>
      </button>
    }
  `,
})
export class AutomationActionEditorComponent {
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
  readonly action = input.required<EditableAutomationAction>();
  readonly canRemove = input(true);
  readonly statuses = input.required<Status[]>();
  readonly users = input.required<WorkspaceAppUser[]>();
  readonly ruleName = input('');
  readonly tags = input.required<Tag[]>();
  readonly sprints = input.required<SprintViewModel[]>();
  readonly boardGroups = input.required<AutomationBoardGroupOption[]>();
  readonly relationTypes = input.required<RelationType[]>();
  readonly defaultStatusId = input<number | null>(null);

  readonly remove = output();
  readonly typeChanged = output<AutomationActionType>();
  readonly patch = output<Partial<EditableAutomationAction>>();

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
