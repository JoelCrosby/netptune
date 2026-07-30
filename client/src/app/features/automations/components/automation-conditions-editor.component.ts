import { Component, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  AutomationConditionGroup,
  AutomationConditionGroupOperator,
  AutomationConditionOperator,
  TaskChangeField,
} from '../models/automation.models';
import { AutomationConditionGroupEditorComponent } from './automation-condition-group-editor.component';

@Component({
  selector: 'app-automation-conditions-editor',
  imports: [
    AutomationConditionGroupEditorComponent,
    BadgeComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <div>
        <div class="flex items-center gap-2">
          <h2 class="font-overpass text-xl font-medium">
            <span
              i18n="
                Heading of the conditions section — the 'if' part of the rule
              ">
              If
            </span>
          </h2>
          <app-badge class="text-[0.65rem] tracking-wide uppercase">
            <span i18n="Marks the conditions section as not required">
              Optional
            </span>
          </app-badge>
        </div>
        <p class="text-foreground/60 text-sm">
          <span i18n="Explains what conditions do">
            Restrict which tasks can continue to the follow-up actions.
          </span>
        </p>
      </div>

      @if (conditionGroup(); as group) {
        <app-automation-condition-group-editor
          [group]="group"
          [statuses]="statuses()"
          [supportsChangeOperators]="supportsChangeOperators()"
          [clearable]="true"
          (cleared)="conditionGroup.set(null)"
          (groupChange)="conditionGroup.set($event)" />
      } @else {
        <div
          class="border-border bg-foreground/2 rounded-lg border border-dashed p-4">
          <p class="mb-1 text-sm font-medium">
            <span i18n="Shown when a rule has no conditions">
              Every matching task will run
            </span>
          </p>
          <p class="text-foreground/60 mb-3 text-sm">
            <span i18n="Advises when conditions are needed">
              Add conditions only when this automation should apply to a smaller
              set of tasks.
            </span>
          </p>
          <button
            app-stroked-button
            type="button"
            (click)="addConditionGroup()">
            <span i18n="Button that starts adding conditions">
              Add conditions
            </span>
          </button>
        </div>
      }
    </div>
  `,
  styles: ``,
})
export class AutomationConditionsEditorComponent {
  statuses = input.required<Status[]>();
  supportsChangeOperators = input(false);
  conditionGroup = model<AutomationConditionGroup | null>(null);

  addConditionGroup() {
    this.conditionGroup.set({
      operator: AutomationConditionGroupOperator.all,
      conditions: [
        {
          field: TaskChangeField.status,
          operator: AutomationConditionOperator.equals,
          value: null,
        },
      ],
      groups: [],
    });
  }
}
