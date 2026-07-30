import { Component, model } from '@angular/core';
import { LucideListChecks, LucideZap } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { IconCircleComponent } from '@static/components/icon-circle.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import {
  taskChangeFieldLabels,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  AutomationTriggerType,
  TaskChangeField,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-trigger-editor',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    BadgeComponent,
    IconCircleComponent,
    PanelComponent,
    PanelHeaderComponent,
    LucideListChecks,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <app-panel
        i18n-aria-label="Accessible name of the trigger panel"
        aria-label="Automation trigger">
        <app-panel-header
          i18n-heading="Heading above the trigger event"
          heading="Trigger event"
          i18n-description="Description of the trigger section"
          description="Choose what starts this automation."
          [icon]="triggerIcon">
          <app-badge
            panelHeaderActions
            color="primary"
            class="text-[0.65rem] font-bold tracking-wider">
            <span i18n="Heading of the trigger part of the rule">WHEN</span>
          </app-badge>
        </app-panel-header>

        <div class="flex min-w-0">
          <div
            class="relative hidden w-16 shrink-0 justify-center sm:flex"
            aria-hidden="true">
            <div
              class="bg-primary/30 absolute top-0 bottom-0 left-1/2 w-px"></div>
            <app-icon-circle
              class="mt-4"
              appearance="solid"
              [icon]="triggerIcon" />
          </div>

          <div class="flex min-w-0 flex-1 flex-col gap-4 p-3 sm:pl-0">
            <app-form-select
              i18n-label="Label of the event field"
              label="Event"
              [noMargin]="true"
              [(value)]="triggerType">
              @for (type of triggerTypes; track type) {
                <app-form-select-option [value]="type">
                  {{ triggerTypeLabel(type) }}
                </app-form-select-option>
              }
            </app-form-select>

            @if (triggerType() === automationTriggerType.taskChanged) {
              <div
                class="border-border bg-foreground/2 overflow-hidden rounded-lg border">
                <div
                  class="border-border flex flex-wrap items-center justify-between gap-2 border-b px-3 py-2.5">
                  <div class="flex items-center gap-2">
                    <svg
                      lucideListChecks
                      class="text-primary h-4 w-4"
                      aria-hidden="true"></svg>
                    <div>
                      <p class="text-sm font-medium">
                        <span
                          i18n="
                            Heading above the fields whose changes trigger the
                            rule
                          ">
                          Watched fields
                        </span>
                      </p>
                      <p class="text-foreground/60 text-xs">
                        <span i18n="Explains the watched fields">
                          Run when any selected field changes.
                        </span>
                      </p>
                    </div>
                  </div>
                  <span
                    class="bg-primary/10 text-primary rounded-full px-2 py-1 text-xs font-semibold">
                    <span
                      i18n="
                        How many watched fields are selected. COUNT is that
                        number
                      ">
                      {{
                        taskFields().length // i18n(ph="COUNT")
                      }}
                      selected
                    </span>
                  </span>
                </div>

                <div class="grid gap-2 p-3 sm:grid-cols-2">
                  @for (field of taskFieldOptions; track field) {
                    <div
                      class="border-border bg-background rounded-md border px-3 py-2.5 transition-colors"
                      [class.border-primary]="hasTaskField(field)"
                      [class.bg-primary/5]="hasTaskField(field)">
                      <app-checkbox
                        [checked]="hasTaskField(field)"
                        (changed)="toggleTaskField(field, $event)">
                        <span class="text-sm">
                          {{ taskFieldLabel(field) }}
                        </span>
                      </app-checkbox>
                    </div>
                  }
                </div>
              </div>
            } @else if (
              triggerType() === automationTriggerType.taskUnassignedFor ||
              triggerType() === automationTriggerType.taskInactiveFor
            ) {
              <div class="border-border bg-foreground/2 rounded-lg border p-3">
                <p class="mb-3 text-sm font-medium">
                  <span i18n="Heading above the delay before a rule runs">
                    Wait period
                  </span>
                </p>
                <div class="flex flex-wrap items-end gap-3">
                  <div class="w-36">
                    <app-form-input
                      i18n-label="Label of the duration field"
                      label="Duration"
                      name="durationDays"
                      type="number"
                      [noMargin]="true"
                      [required]="true"
                      [(value)]="durationDays" />
                  </div>
                  <span class="pb-2.5 text-sm">
                    {{
                      triggerType() === automationTriggerType.taskUnassignedFor
                        ? 'days without an assignee'
                        : 'days without activity'
                    }}
                  </span>
                </div>
              </div>
            } @else if (
              triggerType() === automationTriggerType.sprintEndingSoon
            ) {
              <div class="border-border bg-foreground/2 rounded-lg border p-3">
                <p class="mb-3 text-sm font-medium">
                  <span i18n="Heading above the schedule settings">
                    Schedule
                  </span>
                </p>
                <div class="flex flex-wrap items-end gap-3">
                  <div class="w-36">
                    <app-form-input
                      i18n-label="Label of the lead time field"
                      label="Lead time"
                      name="durationDays"
                      type="number"
                      [noMargin]="true"
                      [required]="true"
                      [(value)]="durationDays" />
                  </div>
                  <span class="pb-2.5 text-sm">
                    <span
                      i18n="
                        Suffix after a number of days, relative to sprint end
                      ">
                      days before the sprint end date
                    </span>
                  </span>
                </div>
                <p class="text-foreground/60 mt-3 text-sm">
                  <span i18n="Explains sprint-scoped rule behaviour">
                    Actions run once for every task in the sprint.
                  </span>
                </p>
              </div>
            } @else if (
              triggerType() === automationTriggerType.taskDueDateApproaching
            ) {
              <div class="border-border bg-foreground/2 rounded-lg border p-3">
                <p class="mb-3 text-sm font-medium">
                  <span i18n="Heading above the schedule settings">
                    Schedule
                  </span>
                </p>
                <div class="flex flex-wrap items-end gap-3">
                  <div class="w-36">
                    <app-form-input
                      i18n-label="Label of the lead time field"
                      label="Lead time"
                      name="durationDays"
                      type="number"
                      [noMargin]="true"
                      [required]="true"
                      [(value)]="durationDays" />
                  </div>
                  <span
                    class="pb-2.5 text-sm"
                    i18n="
                      Suffix after a number of days, relative to the due date
                    ">
                    days before the due date
                  </span>
                </div>
              </div>
            } @else {
              <div class="border-border bg-foreground/2 rounded-lg border p-3">
                <p class="text-sm font-medium">
                  <span i18n="Shown when a trigger needs no further settings">
                    Ready to use
                  </span>
                </p>
                <p class="text-foreground/60 mt-1 text-sm">
                  <span i18n="Explains that no trigger settings are needed">
                    This event does not need additional trigger settings.
                  </span>
                </p>
              </div>
            }
          </div>
        </div>
      </app-panel>
    </div>
  `,
})
export class AutomationTriggerEditorComponent {
  triggerIcon = LucideZap;
  automationTriggerType = AutomationTriggerType;

  triggerTypes = [
    AutomationTriggerType.taskChanged,
    AutomationTriggerType.taskCreated,
    AutomationTriggerType.taskUnassignedFor,
    AutomationTriggerType.taskDueDateApproaching,
    AutomationTriggerType.taskOverdue,
    AutomationTriggerType.taskHasNoDueDate,
    AutomationTriggerType.taskInactiveFor,
    AutomationTriggerType.taskBlocked,
    AutomationTriggerType.taskUnblocked,
    AutomationTriggerType.subtasksCompleted,
    AutomationTriggerType.sprintStarted,
    AutomationTriggerType.sprintCompleted,
    AutomationTriggerType.sprintEndingSoon,
  ];

  taskFieldOptions = [
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

  triggerType = model<AutomationTriggerType>(AutomationTriggerType.taskChanged);
  taskFields = model<TaskChangeField[]>([TaskChangeField.status]);
  durationDays = model('3');

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
  }
}
