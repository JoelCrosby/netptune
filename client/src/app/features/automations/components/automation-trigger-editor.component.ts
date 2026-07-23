import { Component, model } from '@angular/core';
import { LucideListChecks, LucideZap } from '@lucide/angular';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
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
    LucideListChecks,
    LucideZap,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <section
        class="border-border bg-background overflow-hidden rounded-lg border shadow-sm"
        aria-label="Automation trigger">
        <div
          class="border-border bg-foreground/3 flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
          <div class="flex items-center gap-3">
            <span
              class="bg-primary/10 text-primary flex h-8 w-8 items-center justify-center rounded-full">
              <svg lucideZap class="h-4 w-4" aria-hidden="true"></svg>
            </span>
            <div>
              <p class="text-sm font-medium">Trigger event</p>
              <p class="text-foreground/60 text-xs">
                Choose what starts this automation.
              </p>
            </div>
          </div>
          <span
            class="bg-primary/10 text-primary rounded-full px-2.5 py-1 text-[0.65rem] font-bold tracking-wider">
            WHEN
          </span>
        </div>

        <div class="flex min-w-0">
          <div
            class="relative hidden w-16 shrink-0 justify-center sm:flex"
            aria-hidden="true">
            <div
              class="bg-primary/30 absolute top-0 bottom-0 left-1/2 w-px"></div>
            <span
              class="bg-primary text-primary-foreground relative mt-4 flex h-8 w-8 items-center justify-center rounded-full shadow-sm">
              <svg lucideZap class="h-4 w-4"></svg>
            </span>
          </div>

          <div class="flex min-w-0 flex-1 flex-col gap-4 p-3 sm:pl-0">
            <app-form-select
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
                      <p class="text-sm font-medium">Watched fields</p>
                      <p class="text-foreground/60 text-xs">
                        Run when any selected field changes.
                      </p>
                    </div>
                  </div>
                  <span
                    class="bg-primary/10 text-primary rounded-full px-2 py-1 text-xs font-semibold">
                    {{ taskFields().length }} selected
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
              triggerType() === automationTriggerType.taskUnassignedFor
            ) {
              <div class="border-border bg-foreground/2 rounded-lg border p-3">
                <p class="mb-3 text-sm font-medium">Wait period</p>
                <div class="flex flex-wrap items-end gap-3">
                  <div class="w-36">
                    <app-form-input
                      label="Duration"
                      name="durationDays"
                      type="number"
                      [noMargin]="true"
                      [required]="true"
                      [(value)]="durationDays" />
                  </div>
                  <span class="pb-2.5 text-sm">days without an assignee</span>
                </div>
              </div>
            } @else {
              <div class="border-border bg-foreground/2 rounded-lg border p-3">
                <p class="mb-3 text-sm font-medium">Schedule</p>
                <div class="flex flex-wrap items-end gap-3">
                  <div class="w-36">
                    <app-form-input
                      label="Lead time"
                      name="durationDays"
                      type="number"
                      [noMargin]="true"
                      [required]="true"
                      [(value)]="durationDays" />
                  </div>
                  <span class="pb-2.5 text-sm">days before the due date</span>
                </div>
              </div>
            }
          </div>
        </div>
      </section>
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
  readonly triggerType = model<AutomationTriggerType>(
    AutomationTriggerType.taskChanged
  );
  readonly taskFields = model<TaskChangeField[]>([TaskChangeField.status]);
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
  }
}
