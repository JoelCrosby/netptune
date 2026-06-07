import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { LucidePlus, LucideTrash2 } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { CardComponent } from '@static/components/card/card.component';
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
    <app-card class="min-h-0! p-5!">
      <div class="flex flex-col gap-4">
        <div class="flex items-start justify-between gap-3">
          <div>
            <h2 class="text-lg font-semibold">Then</h2>
            <p class="text-muted text-sm">
              Add the follow-up actions in the order they should run.
            </p>
          </div>
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
              <div class="flex items-start gap-3">
                <div
                  class="grid flex-1 gap-3 md:grid-cols-[220px_minmax(0,1fr)]">
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
                  } @else {
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
                  }
                </div>

                <button
                  app-icon-button
                  type="button"
                  title="Remove action"
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
  ];

  readonly actions = input.required<EditableAutomationAction[]>();

  readonly addAction = output();
  readonly removeAction = output<number>();
  readonly actionTypeChanged = output<AutomationActionTypeChange>();
  readonly actionUpdated = output<AutomationActionUpdate>();

  actionTypeLabel(type: AutomationActionType): string {
    return actionTypeLabels[type];
  }
}
