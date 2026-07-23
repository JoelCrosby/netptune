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
          <h2 class="font-overpass text-xl font-medium">If</h2>
          <app-badge class="text-[0.65rem] tracking-wide uppercase">
            Optional
          </app-badge>
        </div>
        <p class="text-foreground/60 text-sm">
          Restrict which tasks can continue to the follow-up actions.
        </p>
      </div>

      @if (conditionGroup(); as group) {
        <app-automation-condition-group-editor
          [group]="group"
          [statuses]="statuses()"
          [clearable]="true"
          (cleared)="conditionGroup.set(null)"
          (groupChange)="conditionGroup.set($event)" />
      } @else {
        <div
          class="border-border bg-foreground/2 rounded-lg border border-dashed p-4">
          <p class="mb-1 text-sm font-medium">Every matching task will run</p>
          <p class="text-foreground/60 mb-3 text-sm">
            Add conditions only when this automation should apply to a smaller
            set of tasks.
          </p>
          <button
            app-stroked-button
            type="button"
            (click)="addConditionGroup()">
            Add conditions
          </button>
        </div>
      }
    </div>
  `,
  styles: ``,
})
export class AutomationConditionsEditorComponent {
  readonly statuses = input.required<Status[]>();
  readonly conditionGroup = model<AutomationConditionGroup | null>(null);

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
