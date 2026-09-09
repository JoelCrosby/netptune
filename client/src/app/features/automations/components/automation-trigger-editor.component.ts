import { Component, computed, model } from '@angular/core';
import { cn } from '@static/components/button/button.variants';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectSearchComponent } from '@static/components/form-select-search/form-select-search.component';
import {
  automationTriggerTypes,
  taskChangeFieldLabels,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  AutomationTriggerType,
  TaskChangeField,
} from '../models/automation.models';
import { AutomationFlowCardComponent } from './automation-flow-card.component';

interface DurationCopy {
  label: string;
  suffix: string;
}

@Component({
  selector: 'app-automation-trigger-editor',
  imports: [
    AutomationFlowCardComponent,
    FormInputComponent,
    FormSelectSearchComponent,
  ],
  template: `
    <app-automation-flow-card
      i18n-keyword="Heading of the trigger part of the rule"
      keyword="WHEN"
      i18n-heading="Heading above the trigger event"
      heading="Trigger event">
      <div class="max-w-105">
        <app-form-select-search
          name="trigger-type"
          i18n-label="Label of the event field"
          label="Event"
          i18n-placeholder="Placeholder in the box that searches trigger events"
          placeholder="Search events"
          i18n-emptyMessage="Shown when no trigger event matches the search"
          emptyMessage="No events found"
          [noMargin]="true"
          [options]="triggerTypes"
          [labelWith]="triggerTypeLabel"
          [value]="triggerType()"
          (changed)="triggerType.set($event)" />
      </div>

      @if (triggerType() === automationTriggerType.taskChanged) {
        <div>
          <div class="mb-2 flex items-baseline justify-between gap-3">
            <span class="text-foreground/55 text-xs font-medium">
              <span
                i18n="Heading above the fields whose changes trigger the rule">
                Watched fields
              </span>
            </span>
            <span class="text-primary text-xs font-semibold">
              <span
                i18n="
                  How many watched fields are selected. COUNT is that number
                ">
                {{
                  taskFields().length // i18n(ph="COUNT")
                }}
                selected
              </span>
            </span>
          </div>

          <div class="flex flex-wrap gap-2">
            @for (field of taskFieldOptions; track field) {
              <button
                type="button"
                [class]="fieldChipClass(field)"
                [attr.aria-pressed]="hasTaskField(field)"
                (click)="toggleTaskField(field)">
                {{ taskFieldLabel(field) }}
              </button>
            }
          </div>
        </div>
      } @else if (durationCopy(); as duration) {
        <div>
          <label
            class="text-foreground/55 mb-1.5 block text-xs font-medium"
            for="durationDays">
            {{ duration.label }}
          </label>
          <div class="flex items-center gap-2.5">
            <div class="w-24">
              <app-form-input
                name="durationDays"
                type="number"
                [noMargin]="true"
                [required]="true"
                [(value)]="durationDays" />
            </div>
            <span class="text-foreground/60 text-sm">{{
              duration.suffix
            }}</span>
          </div>

          @if (triggerType() === automationTriggerType.sprintEndingSoon) {
            <p class="text-foreground/60 mt-2.5 text-sm">
              <span i18n="Explains sprint-scoped rule behaviour">
                Actions run once for every task in the sprint.
              </span>
            </p>
          }
        </div>
      } @else {
        <p
          class="border-border bg-foreground/2 text-foreground/60 rounded-lg border border-dashed px-3 py-2.5 text-[13px]">
          <span i18n="Shown when a trigger needs no further settings">
            This event needs no further settings.
          </span>
        </p>
      }
    </app-automation-flow-card>
  `,
})
export class AutomationTriggerEditorComponent {
  readonly automationTriggerType = AutomationTriggerType;
  readonly triggerTypes = automationTriggerTypes;

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

  protected readonly durationCopy = computed<DurationCopy | null>(() => {
    switch (this.triggerType()) {
      case AutomationTriggerType.taskUnassignedFor:
        return {
          label: $localize`:Label of the wait period before a rule runs:Wait period`,
          suffix: $localize`:Suffix after a number of days without an assignee:days without an assignee`,
        };
      case AutomationTriggerType.taskInactiveFor:
        return {
          label: $localize`:Label of the wait period before a rule runs:Wait period`,
          suffix: $localize`:Suffix after a number of days without activity:days without activity`,
        };
      case AutomationTriggerType.taskDueDateApproaching:
        return {
          label: $localize`:Label of the notice given before a date:Lead time`,
          suffix: $localize`:Suffix after a number of days, relative to the due date:days before the due date`,
        };
      case AutomationTriggerType.sprintEndingSoon:
        return {
          label: $localize`:Label of the notice given before a date:Lead time`,
          suffix: $localize`:Suffix after a number of days, relative to sprint end:days before the sprint end date`,
        };
      default:
        return null;
    }
  });

  readonly triggerTypeLabel = (type: AutomationTriggerType): string => {
    return triggerTypeLabels[type];
  };

  taskFieldLabel(field: TaskChangeField): string {
    return taskChangeFieldLabels[field];
  }

  hasTaskField(field: TaskChangeField): boolean {
    return this.taskFields().includes(field);
  }

  fieldChipClass(field: TaskChangeField): string {
    return cn(
      'focus-visible:ring-primary inline-flex h-8 cursor-pointer items-center rounded-full px-3.5 text-[13px] transition-colors focus-visible:ring-2 focus-visible:outline-none',
      this.hasTaskField(field)
        ? 'bg-primary/15 text-primary font-semibold'
        : 'bg-foreground/5 text-foreground/60 hover:bg-foreground/10 font-medium'
    );
  }

  toggleTaskField(field: TaskChangeField) {
    const fields = this.taskFields();

    this.taskFields.set(
      this.hasTaskField(field)
        ? fields.filter((selected) => selected !== field)
        : [...fields, field]
    );
  }
}
